data "aws_ami" "latest_ubuntu" {
  most_recent = true
  owners = ["099720109477"] # Canonical

  filter {
      name   = "name"
      values = ["ubuntu/images/hvm-ssd/ubuntu-bionic-18.04-amd64-server-*"]
  }

  filter {
      name   = "virtualization-type"
      values = ["hvm"]
  }
}

resource "aws_instance" "simple_chatty_server" {
  ami = data.aws_ami.latest_ubuntu.id
  instance_type = "t2.micro"
  disable_api_termination = false
  instance_initiated_shutdown_behavior = "stop"
  monitoring = false
  iam_instance_profile = aws_iam_instance_profile.simple_chatty_server.name
  subnet_id = aws_subnet.subnet.id

  vpc_security_group_ids = [
    aws_security_group.public_http_https.id
  ]

  root_block_device {
    volume_type = "gp2"
    volume_size = 10
    delete_on_termination = true
  }

  tags = {
    Name = "simple-chatty-server"
  }

  volume_tags = {
    Name = "simple-chatty-server"
  }

  user_data = templatefile("provision.sh", { efs = aws_efs_file_system.simple_chatty_server.dns_name })
}

resource "aws_eip" "simple_chatty_server" {
  instance = aws_instance.simple_chatty_server.id
  vpc = true

  tags = {
    Name = "SimpleChattyServer"
  }  
}

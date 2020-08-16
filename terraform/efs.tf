resource "aws_efs_file_system" "simple_chatty_server" {
  creation_token = "simple-chatty-server"

  tags = {
    Name = "simple-chatty-server"
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_efs_mount_target" "simple_chatty_server" {
  file_system_id = aws_efs_file_system.simple_chatty_server.id
  subnet_id = aws_subnet.subnet.id
  security_groups = [
    aws_security_group.private_nfs.id
  ]
}

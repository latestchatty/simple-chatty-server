# These resources are imported from the default VPC

resource "aws_vpc" "vpc" {
  cidr_block = "172.16.0.0/16"

  # required for EFS
  enable_dns_hostnames = true
  enable_dns_support = true

  lifecycle {
    prevent_destroy = true
    ignore_changes = [
      cidr_block,
      tags
    ]
  }
}

resource "aws_subnet" "subnet" {
  vpc_id = aws_vpc.vpc.id
  cidr_block = "172.16.128.0/24"
  lifecycle {
    prevent_destroy = true
    ignore_changes = [
      cidr_block,
      tags
    ]
  }  
}

resource "aws_security_group" "public_http_https" {
  name = "PublicHttpHttps"
  vpc_id = aws_vpc.vpc.id
  ingress {
    from_port = 443
    to_port = 443
    protocol = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }
  ingress {
    from_port = 80
    to_port = 80
    protocol = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }
  egress {
    from_port = 0
    to_port = 0
    protocol = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }
}


resource "aws_security_group" "private_nfs" {
  name = "PrivateNfs"
  vpc_id = aws_vpc.vpc.id
  ingress {
    from_port = 2049
    to_port = 2049
    protocol = "tcp"
    cidr_blocks = [aws_vpc.vpc.cidr_block]
  }
}

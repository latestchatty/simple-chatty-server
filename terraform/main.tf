terraform {
  backend "s3" {
    bucket = "simple-chatty-server-terraform"
    key = "simple-chatty-server"
    region = "us-east-1"
  }
}

provider "aws" {
  version = "~> 2.19"
  region = "us-east-1"
}

provider "null" {
  version = "~> 2.1"
}

terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
      version = "3.16.0"
    }
    null = {
      source = "hashicorp/null"
      version = "3.0.0"
    }
  }
  backend "s3" {
    bucket = "simple-chatty-server-terraform"
    key = "simple-chatty-server"
    region = "us-east-1"
  }
}

provider "aws" {
  region = "us-east-1"
}

provider "null" {
}


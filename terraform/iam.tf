data "aws_iam_policy_document" "s3_read_access" {
  version = "2012-10-17"

  statement {
    effect = "Allow"
    actions = [
      "s3:GetObject"
    ]
    resources = [
      "arn:aws:s3:::simple-chatty-server",
      "arn:aws:s3:::simple-chatty-server/*"
    ]
  }
}

data "aws_iam_policy_document" "ec2_service_assume_role" {
  version = "2012-10-17"

  statement {
    effect = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "simple_chatty_server" {
  name = "SimpleChattyServer"
  assume_role_policy = data.aws_iam_policy_document.ec2_service_assume_role.json
  description = "simple-chatty-server role"
}

resource "aws_iam_role_policy_attachment" "simple_chatty_server_AmazonSSMManagedInstanceCore" {
  role = aws_iam_role.simple_chatty_server.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_instance_profile" "simple_chatty_server" {
  name = aws_iam_role.simple_chatty_server.name
  role = aws_iam_role.simple_chatty_server.name
}

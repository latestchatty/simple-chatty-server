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

resource "aws_backup_vault" "simple_chatty_server_efs" {
  name = "simple_chatty_server_efs"

  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_backup_plan" "simple_chatty_server_efs" {
  name = "simple_chatty_server_efs"

  rule {
    rule_name = "simple_chatty_server_efs_rule"
    target_vault_name = aws_backup_vault.simple_chatty_server_efs.name
    schedule = "cron(0 6 * * ? *)" # daily at 6 AM

    lifecycle {
      delete_after = 14 # days
    }

    recovery_point_tags = {
      Name = "simple_chatty_server_efs"
    }
  }
}

resource "aws_backup_selection" "simple_chatty_server_efs" {
  plan_id = aws_backup_plan.simple_chatty_server_efs.id
  iam_role_arn = aws_iam_role.backup.arn
  name = "simple_chatty_server_efs"

  resources = [
    aws_efs_file_system.simple_chatty_server.arn
  ]
}

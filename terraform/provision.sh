#!/bin/bash
set -euxo pipefail

# Ubuntu
apt-get update -y
apt-get upgrade -y
apt-get install -y awscli nfs-common
DEBIAN_FRONTEND=noninteractive apt-get remove -y unattended-upgrades

# EFS
mkdir /mnt/efs
echo "${efs}:/ /mnt/efs nfs defaults,_netdev,fsc,nfsvers=4.1,rsize=1048576,wsize=1048576,hard,timeo=600,retrans=2,noresvport 0 0" >> /etc/fstab
mount -a
mkdir -p /mnt/efs/data

# SimpleChattyServer
mkdir /opt/simple-chatty-server
aws s3 cp s3://simple-chatty-server/SimpleChattyServer.gz /opt/simple-chatty-server/
gunzip /opt/simple-chatty-server/SimpleChattyServer.gz
chmod +x /opt/simple-chatty-server/SimpleChattyServer
aws ssm get-parameter --name /SimpleChattyServer/config --query "Parameter.Value" --output text --region us-east-1 --with-decryption > /opt/simple-chatty-server/appsettings.json

# systemd service
echo "SystemMaxUse=100M" >> /etc/systemd/journald.conf
systemctl force-reload systemd-journald

cat >/lib/systemd/system/simple-chatty-server.service <<EOF
[Unit]
Description=SimpleChattyServer
After=network-online.target mnt-efs.mount
Requires=network-online.target mnt-efs.mount

[Service]
Type=simple
Restart=on-failure
RestartSec=30s
WorkingDirectory=/opt/simple-chatty-server
ExecStart=/opt/simple-chatty-server/SimpleChattyServer

[Install]
WantedBy=multi-user.target
EOF

chmod 644 /lib/systemd/system/simple-chatty-server.service
systemctl enable simple-chatty-server.service
systemctl start simple-chatty-server.service

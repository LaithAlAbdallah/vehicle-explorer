#!/bin/bash
# Paste into "User data" when launching the EC2 instance (Amazon Linux 2023); it runs once, as root, on first boot.
# Installs Docker and the Docker Compose plugin.
set -euo pipefail

dnf install -y docker
systemctl enable --now docker
# Lets ec2-user run docker without sudo.
usermod -aG docker ec2-user

mkdir -p /usr/local/lib/docker/cli-plugins
curl -fsSL https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64 \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

#!/bin/bash
# Deploys the latest image from GHCR to the EC2 server. Run from your laptop (Git Bash on Windows):
#   ./deploy/deploy.sh <server-ip> <path-to-key.pem>
set -euo pipefail

SERVER_IP="$1"
KEY_FILE="$2"
SERVER="ec2-user@$SERVER_IP"
SITE_ADDRESS="$SERVER_IP.sslip.io"

cd "$(dirname "$0")"

ssh -i "$KEY_FILE" "$SERVER" "mkdir -p ~/vehicle-explorer"
scp -i "$KEY_FILE" docker-compose.yml Caddyfile "$SERVER:~/vehicle-explorer/"
# Pull the newest image, then start or update the containers.
ssh -i "$KEY_FILE" "$SERVER" \
  "cd ~/vehicle-explorer && export SITE_ADDRESS=$SITE_ADDRESS && docker compose pull && docker compose up -d"

echo "Live at https://$SITE_ADDRESS"

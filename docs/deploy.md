# Deploying to AWS EC2

The app runs on one EC2 server. CI builds the Docker image and pushes it to GitHub Container Registry (GHCR);
the server only pulls that image and runs it, with [Caddy](https://caddyserver.com) in front for automatic HTTPS.
No AWS keys are stored in the repository or in CI.

```
browser ──HTTPS──▶ Caddy (ports 80/443) ──▶ app container (port 8080) ──▶ NHTSA vPIC API
```

## Files

| File | What it does |
|---|---|
| `deploy/server-setup.sh` | Installs Docker and Docker Compose on first boot (EC2 user data). |
| `deploy/docker-compose.yml` | Runs the app image and Caddy on the server. |
| `deploy/Caddyfile` | Tells Caddy to serve `SITE_ADDRESS` over HTTPS and forward requests to the app. |
| `deploy/deploy.sh` | Copies the two files above to the server and starts the newest image. |

## One-time setup

1. **Make the image public.** On GitHub: your profile → Packages → `vehicle-explorer` → Package settings →
   Change visibility → Public. The server can then pull it without logging in.
2. **Budget alert.** AWS Billing → Budgets → Create budget → monthly cost budget of **$5** with an email alert.
3. **Launch the instance.** EC2 → Launch instance:
   - Name `vehicle-explorer`, image **Amazon Linux 2023**, type **t3.small**.
   - Key pair: create one (`.pem`) and keep it outside the repository.
   - Network settings → security group with three inbound rules:
     SSH (22) from your IPv4 address as `x.x.x.x/32`, HTTP (80) from anywhere, HTTPS (443) from anywhere.
     Check your IPv4 address with `curl https://checkip.amazonaws.com`: the console's "My IP" may pick
     your IPv6 address, which cannot reach an IPv4-only instance.
   - Advanced details → User data: paste the contents of `deploy/server-setup.sh`.
4. **Fixed address.** EC2 → Elastic IPs → Allocate → Associate with the instance.
   The site will be `https://<elastic-ip>.sslip.io` ([sslip.io](https://sslip.io) maps that name to the IP,
   which lets Caddy get a certificate without buying a domain).

## Deploy (first time and after every change on `main`)

Wait until the CI run on `main` is green, then from the repository root in Git Bash:

```bash
./deploy/deploy.sh <elastic-ip> <path-to-key.pem>
```

The script prints the live link. The first HTTPS request can take a few seconds while Caddy gets the certificate.

## Checks and troubleshooting

```bash
ssh -i <path-to-key.pem> ec2-user@<elastic-ip>
cd ~/vehicle-explorer
docker compose ps            # both containers should be "running"
docker compose logs caddy    # certificate problems show up here
docker compose logs app      # API errors and NHTSA warm-up
```

- SSH times out: your home IP has probably changed; update the SSH rule in the security group.
- Both containers use `restart: unless-stopped`, so they come back after a server reboot.
- Behind Caddy every request comes from Caddy's address, so the app trusts the `X-Forwarded-For` header
  (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`) to rate-limit each user by their real IP. The app port is not published,
  so only Caddy can reach it.

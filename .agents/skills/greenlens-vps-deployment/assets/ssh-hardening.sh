#!/usr/bin/env bash
# =============================================================================
# Greenlens — Ubuntu 24.04 first-time VPS hardening
# Run ONCE as root immediately after VPS provisioning.
#
# Usage:
#   ssh root@<VPS_IP>
#   curl -fsSL https://raw.githubusercontent.com/<owner>/greenlens/main/deploy/ssh-hardening.sh | bash
# OR
#   scp ssh-hardening.sh root@<VPS_IP>:/tmp/
#   ssh root@<VPS_IP> "bash /tmp/ssh-hardening.sh"
#
# After this script:
#   - new user `appuser` with sudo, key-based SSH only
#   - root SSH login disabled
#   - password auth disabled
#   - ufw enabled, only port 22 inbound
#   - automatic security updates enabled
#   - Docker + cloudflared installed
# =============================================================================

set -euo pipefail

# ---- Config — EDIT THESE BEFORE RUNNING ----
APP_USER="appuser"
SSH_PUBLIC_KEY="ssh-ed25519 AAAA... your-key-here"          # paste your public key
SSH_PORT=22                                                  # keep 22 to simplify Cloudflare Tunnel SSH flow

# ---- Sanity ----
if [[ $EUID -ne 0 ]]; then
   echo "ERROR: Run as root" ; exit 1
fi
if [[ "$SSH_PUBLIC_KEY" == *"your-key-here"* ]]; then
    echo "ERROR: Edit SSH_PUBLIC_KEY in this script first" ; exit 1
fi

echo "==> [1/8] System update"
apt-get update -y
DEBIAN_FRONTEND=noninteractive apt-get upgrade -y
apt-get install -y curl wget git ufw unattended-upgrades fail2ban

echo "==> [2/8] Create non-root user '$APP_USER'"
if ! id -u "$APP_USER" >/dev/null 2>&1; then
    adduser --disabled-password --gecos "" "$APP_USER"
    usermod -aG sudo "$APP_USER"
fi
mkdir -p "/home/$APP_USER/.ssh"
echo "$SSH_PUBLIC_KEY" > "/home/$APP_USER/.ssh/authorized_keys"
chmod 700 "/home/$APP_USER/.ssh"
chmod 600 "/home/$APP_USER/.ssh/authorized_keys"
chown -R "$APP_USER:$APP_USER" "/home/$APP_USER/.ssh"

# Allow sudo without password for deploy actions (CI/CD)
echo "$APP_USER ALL=(ALL) NOPASSWD: /usr/bin/docker, /usr/bin/docker-compose, /usr/bin/systemctl" \
    > /etc/sudoers.d/90-appuser-deploy
chmod 440 /etc/sudoers.d/90-appuser-deploy

echo "==> [3/8] Harden SSH"
SSHD_CONFIG=/etc/ssh/sshd_config
sed -i 's/^#\?PermitRootLogin.*/PermitRootLogin no/' "$SSHD_CONFIG"
sed -i 's/^#\?PasswordAuthentication.*/PasswordAuthentication no/' "$SSHD_CONFIG"
sed -i 's/^#\?PubkeyAuthentication.*/PubkeyAuthentication yes/' "$SSHD_CONFIG"
sed -i 's/^#\?ChallengeResponseAuthentication.*/ChallengeResponseAuthentication no/' "$SSHD_CONFIG"
sed -i "s/^#\?Port .*/Port $SSH_PORT/" "$SSHD_CONFIG"
systemctl restart ssh

echo "==> [4/8] UFW firewall — only port 22 inbound, all outbound allowed"
ufw default deny incoming
ufw default allow outgoing
ufw allow "$SSH_PORT/tcp"
# NOTE: NO `ufw allow 80/443` — Cloudflare Tunnel uses outbound TCP/443 only.
echo "y" | ufw enable
ufw status verbose

echo "==> [5/8] fail2ban (SSH bruteforce protection)"
systemctl enable --now fail2ban

echo "==> [6/8] Automatic security updates"
dpkg-reconfigure -plow unattended-upgrades || true
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'EOF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Download-Upgradeable-Packages "1";
APT::Periodic::AutocleanInterval "7";
APT::Periodic::Unattended-Upgrade "1";
EOF

echo "==> [7/8] Install Docker (official repo)"
if ! command -v docker >/dev/null 2>&1; then
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
        | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg

    UBUNTU_CODENAME=$(. /etc/os-release && echo "$VERSION_CODENAME")
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
        https://download.docker.com/linux/ubuntu $UBUNTU_CODENAME stable" \
        > /etc/apt/sources.list.d/docker.list

    apt-get update -y
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi
usermod -aG docker "$APP_USER"
systemctl enable --now docker

echo "==> [8/8] Install cloudflared (Cloudflare Tunnel)"
if ! command -v cloudflared >/dev/null 2>&1; then
    curl -fsSL https://pkg.cloudflare.com/cloudflare-main.gpg \
        | tee /usr/share/keyrings/cloudflare-main.gpg >/dev/null
    echo "deb [signed-by=/usr/share/keyrings/cloudflare-main.gpg] https://pkg.cloudflare.com/cloudflared $(lsb_release -cs) main" \
        | tee /etc/apt/sources.list.d/cloudflared.list >/dev/null
    apt-get update -y
    apt-get install -y cloudflared
fi

echo "==> Creating /opt/greenlens (deploy target)"
mkdir -p /opt/greenlens/{logs,backups}
chown -R "$APP_USER:$APP_USER" /opt/greenlens

echo
echo "================ DONE ================"
echo "Next steps:"
echo "  1. Test new login: ssh $APP_USER@<VPS_IP>  (from your laptop)"
echo "  2. Create Cloudflare Tunnel: see runbook-phase1-iponly.md"
echo "  3. Copy docker-compose.yml + .env.production to /opt/greenlens/"
echo "  4. Configure GitHub Actions deploy workflow"
echo
echo "Verify:"
echo "  ssh -o PasswordAuthentication=no $APP_USER@<VPS_IP>  # should work"
echo "  ssh root@<VPS_IP>                                     # should be DENIED"
echo "  sudo ufw status                                       # only 22 allowed"
echo "======================================="

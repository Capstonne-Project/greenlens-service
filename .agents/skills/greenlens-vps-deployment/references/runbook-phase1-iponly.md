# Runbook — Phase 1: IP-only Deploy (chưa có domain)

> Mục tiêu: từ một VPS Ubuntu 24.04 mới toanh → BE Greenlens chạy được, FE/mobile gọi API qua URL HTTPS `https://<random>.cfargotunnel.com`. Không cần domain. Toàn bộ ~30 phút.

## Tổng quan kiến trúc Phase 1

```
   ┌─────────────────────────────────────┐
   │  FE / Mobile (Internet)             │
   └──────────────┬──────────────────────┘
                  │ HTTPS
                  ▼
   ┌─────────────────────────────────────┐
   │  Cloudflare Edge                    │
   │  https://<uuid>.cfargotunnel.com    │   ← URL Cloudflare cấp
   └──────────────┬──────────────────────┘
                  │ outbound TCP/443
                  │ (Tunnel — KHÔNG có inbound)
                  ▼
   ┌─────────────────────────────────────┐
   │  VPS Ubuntu 24.04                   │
   │  ┌─────────────┐  ┌─────────────┐   │
   │  │ cloudflared │  │ docker      │   │
   │  │ (host)      │→│ compose     │   │
   │  └─────────────┘  │ ┌─────────┐ │   │
   │                   │ │ API     │ │   │
   │                   │ │ :8080   │ │   │
   │                   │ └────┬────┘ │   │
   │                   │   ┌──┴──┐   │   │
   │                   │   │PG, R│   │   │
   │                   │   └─────┘   │   │
   │                   └─────────────┘   │
   │  ufw: chỉ port 22 inbound           │
   └─────────────────────────────────────┘
```

**Key:** Cloudflare Tunnel **outbound-only** từ VPS → Cloudflare. KHÔNG cần mở port 80/443 trên ufw. KHÔNG cần public IP whitelisting.

---

## Phase 1 step-by-step

### Step 0 — Prereqs (5 phút)

- [ ] VPS Ubuntu 24.04 đã có (provider: DigitalOcean, Vultr, Hetzner, BizFly, Viettel IDC — đều OK)
- [ ] Có IP public của VPS, và password root từ provider
- [ ] Tài khoản Cloudflare (free OK) — https://dash.cloudflare.com/sign-up
- [ ] SSH keypair trên máy local — nếu chưa có:
      ```bash
      ssh-keygen -t ed25519 -C "your-email@example.com"
      cat ~/.ssh/id_ed25519.pub      # copy line này, sẽ paste vào script hardening
      ```

### Step 1 — Hardening + cài Docker, cloudflared (10 phút)

1. SSH lần đầu vào VPS bằng password:
   ```bash
   ssh root@<VPS_IP>
   ```

2. Tải script hardening và edit phần `SSH_PUBLIC_KEY`:
   ```bash
   curl -fsSL https://raw.githubusercontent.com/<your-repo>/main/deploy/ssh-hardening.sh -o /tmp/harden.sh
   nano /tmp/harden.sh
   # Paste public key vào dòng SSH_PUBLIC_KEY="..."
   bash /tmp/harden.sh
   ```

   (Hoặc nếu chưa có repo, scp file từ asset của skill này lên VPS trước.)

3. **TRƯỚC KHI EXIT root session**, mở terminal khác và verify login mới:
   ```bash
   ssh appuser@<VPS_IP>
   # Phải login được không cần password
   ```
   Nếu lỗi → quay lại root session fix `~/.ssh/authorized_keys`. **Không bao giờ exit root khi chưa verify login mới.**

4. Verify:
   ```bash
   ssh appuser@<VPS_IP>
   docker --version            # Docker version 27.x
   cloudflared --version       # cloudflared version 2024.x
   sudo ufw status verbose     # chỉ ALLOW IN 22/tcp
   ```

### Step 2 — Tạo Cloudflare Tunnel (5 phút)

1. Vào Cloudflare dashboard → Zero Trust → Networks → Tunnels → Create a tunnel.
2. Chọn **Cloudflared** → Next.
3. Tunnel name: `greenlens-vps` → Save tunnel.
4. Chọn environment **Debian** (Ubuntu cùng family) → copy lệnh chứa `--token eyJh...`.
5. **Không** chạy lệnh ấy nguyên xi (nó dùng curl tải binary lại). Tách lấy token thôi.

6. Trên VPS:
   ```bash
   ssh appuser@<VPS_IP>
   sudo mkdir -p /etc/cloudflared
   sudo bash -c 'echo "TUNNEL_TOKEN=eyJh..." > /etc/cloudflared/tunnel.env'
   sudo chmod 600 /etc/cloudflared/tunnel.env

   # Install as systemd service
   sudo cloudflared service install eyJh...      # paste token at the end
   sudo systemctl status cloudflared              # active (running)
   ```

7. Quay lại Cloudflare dashboard, tunnel `greenlens-vps` sẽ chuyển status **HEALTHY** trong ~30s.

### Step 3 — Configure Tunnel ingress (3 phút)

Trên Cloudflare dashboard → tunnel `greenlens-vps` → **Public Hostname** tab → **Add a public hostname**:

| Field | Value |
|---|---|
| Subdomain | (để trống) |
| Domain | chọn `<uuid>.cfargotunnel.com` (option mặc định Cloudflare cho khi chưa có domain) |
| Service Type | `HTTP` |
| URL | `localhost:8080` |

Save. Cloudflare cấp ngay URL `https://<uuid>.cfargotunnel.com`.

> **Lưu ý:** Tab "Public Hostname" chỉ cho gắn domain đã có trên Cloudflare account. Nếu chưa có domain **nào**, dùng **Quick Tunnel** thay thế:
> ```bash
> cloudflared tunnel --url http://localhost:8080
> # → URL kiểu https://<random>.trycloudflare.com (ephemeral — đổi mỗi lần restart)
> ```
> Hoặc đăng ký 1 domain $1/năm (Namecheap có `.click`, `.xyz` rẻ) → add vào Cloudflare account → giờ có thể tạo subdomain trên đó qua tunnel ổn định.

### Step 4 — Deploy lần đầu (manual, ~5 phút)

Tại sao manual lần đầu? Để verify pipeline trước khi tự động hoá.

1. Trên máy local:
   ```bash
   # Build image local
   docker build -t greenlens-api:bootstrap .

   # Save + scp lên VPS (vì chưa có GHCR)
   docker save greenlens-api:bootstrap | gzip > /tmp/api.tar.gz
   scp /tmp/api.tar.gz appuser@<VPS_IP>:/tmp/
   scp docker-compose.yml appuser@<VPS_IP>:/opt/greenlens/
   scp .env.production appuser@<VPS_IP>:/opt/greenlens/
   ```

2. Trên VPS:
   ```bash
   ssh appuser@<VPS_IP>
   cd /opt/greenlens
   chmod 600 .env.production
   docker load < /tmp/api.tar.gz                    # load image
   docker tag greenlens-api:bootstrap ghcr.io/<owner>/greenlens-api:latest

   # Khởi động
   docker compose --env-file .env.production up -d postgres redis
   sleep 10                                          # wait for postgres healthy

   # Run migrations
   docker compose --env-file .env.production run --rm migrator

   # Start API
   docker compose --env-file .env.production up -d api

   # Watch logs
   docker compose logs -f api
   ```

3. Verify từ máy local:
   ```bash
   curl https://<your-tunnel-url>/health
   # → {"status":"Healthy"}
   ```

🎉 **Phase 1 done.** FE/mobile dùng `<your-tunnel-url>` làm API base.

### Step 5 — Setup GitHub Actions (5 phút)

1. Repo → Settings → Secrets and variables → Actions → New repository secret:

   | Name | Value |
   |---|---|
   | `VPS_HOST` | IP VPS |
   | `VPS_USER` | `appuser` |
   | `VPS_SSH_KEY` | content của `~/.ssh/id_ed25519` (private key) |

2. Copy `.github/workflows/deploy.yml` từ asset vào repo.

3. Commit + push tới `main`:
   ```bash
   git add .github/ Dockerfile docker-compose.yml
   git commit -m "ci: add deploy pipeline"
   git push
   ```

4. Theo dõi: Repo → Actions tab → workflow `Deploy to VPS` chạy. Sau ~5 phút → status xanh.

5. Verify image đã ở GHCR: Repo → Packages tab → `greenlens-api`.

6. Test redeploy: edit file bất kỳ trong `src/`, commit, push → workflow chạy lại tự động.

---

## Sanity checks (làm sau setup)

```bash
# Trên VPS:
ssh appuser@<VPS_IP>

# 1) Tất cả container healthy
docker compose ps
# → 3 services Up, status (healthy)

# 2) API chỉ listen localhost
sudo ss -tlnp | grep 8080
# → 127.0.0.1:8080 (KHÔNG được là 0.0.0.0:8080)

# 3) Postgres/Redis không expose
sudo ss -tlnp | grep -E "5432|6379"
# → KHÔNG có gì (empty)

# 4) ufw kín
sudo ufw status
# → 22/tcp ALLOW IN, nothing else

# 5) Cloudflare Tunnel healthy
sudo systemctl status cloudflared
# → active (running)

# 6) Logs sạch
docker compose logs --tail=50 api
# → không có ERROR

# Từ máy local:
curl -I https://<tunnel-url>/health
# → HTTP/2 200
```

---

## Khi nào sang Phase 2

Khi anh mua domain. Đọc `runbook-phase2-domain.md`.

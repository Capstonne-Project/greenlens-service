---
name: greenlens-vps-deployment
description: Deploy the Greenlens .NET 9 backend to a Ubuntu 24.04 VPS using Docker Compose + Cloudflare Tunnel + GitHub Actions CI/CD for project SU26SE049. Use this skill whenever the user asks about deployment, hosting, VPS setup, Docker, "đưa BE lên server", "deploy lên VPS", SSH hardening, GitHub Actions, CI/CD pipeline, Cloudflare Tunnel, "expose API ra internet", "có HTTPS chưa có domain", or talks about moving from local to a public server. Covers BOTH phases: phase 1 (no domain yet, access via Cloudflare-provided trycloudflare.com URL), phase 2 (after buying a domain, configure DNS + production Tunnel route). Produces Dockerfile, docker-compose.yml, env templates, systemd unit for the Tunnel, GitHub Actions workflow, and runbooks for first-time setup + every deploy after.
---

# Greenlens VPS Deployment

Deploy stack chốt cho dự án Greenlens (capstone SU26SE049):

| Layer | Tech | Why |
|---|---|---|
| **VPS** | Ubuntu 24.04 LTS x64 | LTS đến 2029, snap removed của 24.04 sạch hơn |
| **Runtime** | Docker + docker compose v2 | Reproducible, dev/prod parity |
| **Containers** | API + Postgres 18 + Redis | Cùng compose, Postgres + Redis chỉ bind `127.0.0.1` |
| **Edge** | Cloudflare Tunnel (`cloudflared`) | KHÔNG mở port 80/443 ra internet; có HTTPS ngay cả khi chưa có domain |
| **CI/CD** | GitHub Actions → SSH deploy | Build image trên runner, push tới GHCR, pull về VPS, restart |
| **Secrets** | `.env.production` (chmod 600) trên VPS + GitHub Secrets cho CI | Capstone scope — không vendor key vault |

## When to use

Trigger when the user mentions:
- "deploy", "VPS", "server", "hosting", "production"
- "Docker", "Dockerfile", "compose"
- "CI/CD", "pipeline", "GitHub Actions", "auto deploy"
- "Cloudflare Tunnel", "cloudflared"
- "SSH", "hardening", "ufw", "firewall"
- "domain", "DNS", "HTTPS chưa có domain"
- "đưa BE lên server", "publish API"

## Two phases — match the user's situation

### Phase 1: IP-only (chưa có domain)

Triệu chứng: user vừa mua VPS, chưa có domain, muốn FE/mobile test được API.

Flow:
1. SSH hardening (key auth, no root login, ufw allow chỉ port 22 + 22 outbound TCP/443 cho Tunnel)
2. Install Docker + cloudflared
3. Tạo Cloudflare Tunnel → token gắn vào systemd service
4. Run `docker compose up -d` → API listen `127.0.0.1:8080`
5. Tunnel route `127.0.0.1:8080` → Cloudflare cấp URL `https://<random>.trycloudflare.com` (HTTPS sẵn)
6. FE/mobile dùng URL này làm `API_BASE_URL`

Pros: HTTPS ngay, không cần mở port public, không phải mua domain để test.
Cons: URL `trycloudflare.com` ephemeral (đổi mỗi lần restart tunnel nếu dùng "Quick Tunnel"). Skill setup **Named Tunnel** stable từ đầu — URL không đổi, nhưng phải đăng nhập Cloudflare account 1 lần.

Use `assets/runbook-phase1-iponly.md` cho hướng dẫn từng bước.

### Phase 2: After buying domain

Triệu chứng: user đã có domain (`greenlens.com`), muốn `api.greenlens.com` trỏ về VPS.

Flow:
1. Add domain vào Cloudflare account (nameservers → Cloudflare)
2. Cloudflare dashboard → Tunnels → edit existing tunnel → thêm public hostname `api.greenlens.com` → `http://localhost:8080`
3. DNS record `api.greenlens.com` được tunnel tự tạo (orange-cloud proxied) — OVERVIEW.md §14.1
4. Bật Authenticated Origin Pulls (mTLS) — OVERVIEW.md §14.5 — vì giờ traffic là production
5. Update FE/mobile `API_BASE_URL` → `https://api.greenlens.com`

Use `assets/runbook-phase2-domain.md`.

**Quan trọng:** giữa 2 phase **không phải build lại Docker image hay đổi code BE**. Tunnel + DNS là edge config, BE không biết URL public của mình.

## Workflow when user asks

1. **Identify phase** — hỏi nếu chưa rõ:
   - "Anh đã mua domain chưa? Nếu chưa, mình setup Phase 1 (IP-only qua Cloudflare Tunnel) trước, lúc nào có domain thì add vào Tunnel mà không phải redeploy."

2. **First-time setup vs ongoing deploy** — 2 quy trình khác nhau:
   - First-time: SSH hardening + install Docker + Tunnel + secrets → ~30 phút
   - Ongoing: git push → GitHub Actions tự build + deploy → ~5 phút

3. **Pick assets:**
   - `assets/Dockerfile` — multi-stage .NET 9 build, non-root user
   - `assets/docker-compose.yml` — API + Postgres 18 + Redis, all internal network
   - `assets/.env.production.template` — env vars cần fill
   - `assets/cloudflared.service` — systemd unit cho tunnel
   - `assets/github-actions-deploy.yml` — `.github/workflows/deploy.yml`
   - `assets/nginx-internal.conf` — OPTIONAL nếu user muốn reverse proxy nội bộ (mặc định Tunnel route thẳng vào container — không cần)
   - `references/runbook-phase1-iponly.md` — step-by-step IP-only setup
   - `references/runbook-phase2-domain.md` — migrate sang domain
   - `references/troubleshooting.md` — log locations, common errors, rollback

4. **Walk through the runbook** với user nếu họ chưa từng deploy. Mỗi bước có:
   - Command để chạy
   - Expected output
   - "Nếu thấy X thay vì Y, đó là vấn đề Z, fix bằng W"

## Conventions enforced

### Dockerfile

- Multi-stage: `sdk:9.0` để build, `aspnet:9.0` để runtime → image ~120MB thay vì ~800MB
- Non-root user `app` (uid 1000) — KHÔNG chạy là root trong container
- `HEALTHCHECK` curl `/health` endpoint (OVERVIEW.md §9 healthcheck)
- `EXPOSE 8080` — bind trong-container, không công khai
- `ENV ASPNETCORE_URLS=http://+:8080`
- Cache `dotnet restore` ở layer riêng để rebuild nhanh

### docker-compose.yml

- **3 services:** `api`, `postgres`, `redis`
- **Networking:**
  - `api` ports: `127.0.0.1:8080:8080` — chỉ localhost trên VPS (Cloudflare Tunnel sẽ proxy)
  - `postgres`, `redis`: **không expose port ra host** (internal network only)
- **Postgres image:** `postgis/postgis:18-3.5` (cần PostGIS theo OVERVIEW.md §2)
- **Volumes:**
  - `postgres_data` — persistent
  - `redis_data` — persistent
  - `./logs:/app/logs` — bind mount để xem log dễ
- **Restart policy:** `unless-stopped`
- **Healthchecks** trên cả 3 service — `api` depends_on postgres+redis với `condition: service_healthy`

### CI/CD (GitHub Actions)

Workflow 2 job:

1. **build-and-push:** trên push tới `main`:
   - dotnet test
   - docker build → push to `ghcr.io/<user>/greenlens-api:${{ github.sha }}` + `:latest`
2. **deploy:** sau build:
   - SSH vào VPS (key trong GitHub Secrets)
   - `docker compose pull && docker compose up -d`
   - Run EF migrations: `docker compose exec api dotnet ef database update`
   - Smoke test: `curl localhost:8080/health` → must return 200
   - Nếu fail → tự động rollback (`docker compose up -d` với image trước đó)

**GitHub Secrets cần:**
- `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY` — deploy target
- `GHCR_PAT` — Personal Access Token có scope `write:packages` (alternative: dùng `GITHUB_TOKEN` mặc định, đủ quyền cho package cùng repo)

### Secrets on VPS

- `/opt/greenlens/.env.production` — owner `appuser:appuser`, mode `600`
- KHÔNG commit `.env*` vào git
- `docker-compose.yml` đọc qua `env_file: .env.production`
- Khi rotate secret: `nano .env.production` → `docker compose up -d` (compose tự pickup env mới)

## Self-check

- [ ] User đã chọn Phase 1 hay Phase 2
- [ ] Phase 1: Cloudflare account đã có (free OK), Named Tunnel đã tạo
- [ ] Dockerfile multi-stage, non-root user, healthcheck
- [ ] docker-compose Postgres + Redis KHÔNG bind ra `0.0.0.0`
- [ ] ufw: chỉ allow port 22 inbound (đóng 80, 443 vì Tunnel không cần — tunnel outbound TCP/443)
- [ ] SSH: key-based only, `PermitRootLogin no`, `PasswordAuthentication no`
- [ ] GitHub Actions có rollback step
- [ ] `.env.production` chmod 600
- [ ] Phase 2 (có domain): Authenticated Origin Pulls bật (OVERVIEW.md §14.5)

## Common pitfalls

| Pitfall | Why bad | Fix |
|---|---|---|
| Bind Postgres `5432:5432` ra host | Port lộ ra internet → bruteforce | `127.0.0.1:5432:5432` hoặc tốt hơn: bỏ section `ports` luôn, chỉ dùng internal network |
| Chạy container là root | Privilege escalation nếu CVE | Dockerfile `USER app` |
| `appsettings.Production.json` chứa connection string | Lộ secret khi `cat` | Dùng env var qua `${ConnectionStrings__Postgres}` placeholder |
| GitHub Actions chạy `docker compose down && up` | Downtime ~10s + risk Postgres orphan | Dùng `up -d` (compose tự rolling) hoặc blue-green với 2 compose project nếu cần zero-downtime |
| Migrations chạy trong app startup | 2 replica → race condition; lỗi migration làm pod CrashLoop | Migrations chạy thành **bước riêng** trong CI (như workflow) hoặc job riêng (`migrator` service in compose, `restart: "no"`) |
| Mở port 80/443 trên ufw "cho chắc" | Bypass Cloudflare → mất WAF protection | Tunnel làm việc qua outbound; không cần inbound 80/443. Đóng cả 2 |
| Save Cloudflare Tunnel token vào git | Token cho phép control tunnel | Token vào `.env.production` (mode 600) hoặc `/etc/cloudflared/cert.pem` (`cloudflared service install` tự handle) |
| Để Postgres dùng password mặc định "postgres" | Lộ qua docker inspect | Generate strong password vào `.env.production` ngay từ setup đầu |

## Templates list

- `assets/Dockerfile`
- `assets/docker-compose.yml`
- `assets/.env.production.template`
- `assets/cloudflared.service`
- `assets/github-actions-deploy.yml`
- `assets/ssh-hardening.sh`
- `references/runbook-phase1-iponly.md` — first-time IP-only setup, từng lệnh một
- `references/runbook-phase2-domain.md` — migrate sang domain mà không redeploy
- `references/troubleshooting.md`

## Example interaction

**User:** "Tôi vừa mua VPS Ubuntu 24.04, chưa có domain. Làm sao deploy BE lên đó để team FE test API?"

**Your response:**
1. Confirm Phase 1 path (Cloudflare Tunnel + IP-only).
2. Hỏi: "Anh đã có Cloudflare account chưa? Nếu chưa, tạo free account trước (5 phút). Nếu có rồi, mình bắt đầu hardening SSH và install Docker."
3. Load `references/runbook-phase1-iponly.md` và walk through từng bước.
4. Generate Dockerfile + docker-compose + .env.production.template cho repo.
5. Setup GitHub Actions workflow.
6. Test deploy lần đầu manually từ máy local (`scp` files + `docker compose up -d` qua SSH).
7. Push tới `main` → GitHub Actions chạy tự động → verify URL `https://<random>.trycloudflare.com` (hoặc Named Tunnel URL nếu setup từ đầu) trả về `/health` 200.

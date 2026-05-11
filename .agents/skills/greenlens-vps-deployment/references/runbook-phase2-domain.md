# Runbook — Phase 2: Migrate sang Domain thật

> Mục tiêu: từ `https://<uuid>.cfargotunnel.com` (Phase 1) → `https://api.greenlens.example` (Phase 2).
> **KHÔNG cần redeploy** — toàn bộ là edge config trên Cloudflare. BE không biết URL public của mình.

## Tổng quan thay đổi

| | Phase 1 | Phase 2 |
|---|---|---|
| URL public | `<uuid>.cfargotunnel.com` | `api.greenlens.example` |
| DNS record | Cloudflare tự tạo | Bạn add qua Cloudflare DNS UI (hoặc Tunnel tự thêm) |
| TLS cert | Cloudflare-managed (`*.cfargotunnel.com` shared) | Cloudflare Universal SSL (auto, free) cho domain riêng |
| Authenticated Origin Pulls (mTLS) | KHÔNG (Tunnel đã secure) | KHÔNG cần (Tunnel vẫn xài) |
| Production hardening (WAF, rate limit, Turnstile) | Tùy chọn | Bật theo OVERVIEW.md §14 |

> **Lưu ý:** Authenticated Origin Pulls (OVERVIEW.md §14.5) áp dụng cho deploy DNS+orange-cloud thẳng (server expose 443 ra internet). Khi dùng **Cloudflare Tunnel**, traffic CF→origin đã đi qua kết nối tunnel outbound do `cloudflared` khởi tạo — KHÔNG còn route từ internet vào origin trực tiếp → không cần mTLS thêm. Cảnh báo bypass scanner trong §14.5 cũng không áp dụng (không có open port để scan).

## Phase 2 step-by-step

### Step 1 — Mua domain + add vào Cloudflare (15 phút)

1. Mua domain ở Namecheap / Porkbun / GoDaddy / một registrar bất kỳ.
2. Cloudflare dashboard → Add a Site → enter domain → chọn **Free plan** → Continue.
3. Cloudflare scan DNS hiện tại (thường rỗng) → Continue.
4. Cloudflare hiển thị 2 nameservers, ví dụ `ana.ns.cloudflare.com`, `bob.ns.cloudflare.com`.
5. Trên trang registrar → đổi nameservers của domain thành 2 NS đó → save.
6. Propagation: 5 phút đến 24 giờ (thường < 1 giờ). Status trên Cloudflare → "Active".

### Step 2 — Add public hostname vào tunnel hiện tại (3 phút)

Cloudflare dashboard → Zero Trust → Networks → Tunnels → `greenlens-vps` → Public Hostname tab → Add a public hostname:

| Field | Value |
|---|---|
| Subdomain | `api` |
| Domain | `greenlens.example` (domain anh vừa add) |
| Path | (để trống) |
| Service Type | `HTTP` |
| URL | `localhost:8080` |

Save. Cloudflare tự:
- Tạo DNS record `api.greenlens.example` CNAME → `<tunnel-uuid>.cfargotunnel.com` (proxied, orange-cloud).
- Cấp TLS cert qua Universal SSL.

Sau ~30s:
```bash
curl https://api.greenlens.example/health
# → {"status":"Healthy"}
```

### Step 3 — Cập nhật FE/mobile + GitHub Actions (5 phút)

1. FE/mobile config `API_BASE_URL=https://api.greenlens.example`.
2. GitHub repo → Settings → Secrets and variables → Actions → Variables tab:
   - Add `PUBLIC_API_URL = https://api.greenlens.example`
   - Workflow `deploy.yml` đã có sẵn smoke-test step đọc biến này, bật bằng cách uncomment.

### Step 4 — Bật production hardening trên Cloudflare (10 phút)

Tham chiếu OVERVIEW.md §14:

#### 4.1 WAF + Rate Limit (§14.3)

Cloudflare dashboard → `greenlens.example` → Security → WAF:
- Bật **Managed Rules** (default OWASP rules).
- Custom rules:
  | Rule | Action |
  |---|---|
  | `(http.request.uri.path eq "/api/v1/auth/login")` + threshold 5/min/IP | Challenge (Turnstile) |
  | `(http.request.uri.path eq "/api/v1/reports") and http.request.method eq "POST"` + threshold 10/min/IP | Block |

Lưu ý: rate-limit ở edge **chạy trước** app rate-limit (OVERVIEW.md §13.8 nói "2 tầng"). App rate-limit là lưới an toàn nếu CF không bắt được.

#### 4.2 Turnstile (BR-AUTH-011)

- Cloudflare dashboard → Turnstile → Add a site → domain `greenlens.example` → copy Site Key + Secret Key.
- Cập nhật `.env.production` trên VPS:
  ```
  TURNSTILE_SITE_KEY=0x4AAA...
  TURNSTILE_SECRET_KEY=0x4AAA...
  ```
- Restart API: `docker compose up -d api` (compose tự pick up env mới).
- FE nhúng widget với `data-sitekey` = `TURNSTILE_SITE_KEY` ở `/login` (sau 3 lần fail per BR-AUTH-011).

#### 4.3 Custom domain cho R2 / media (§14.2)

- Cloudflare R2 dashboard → bucket `greenlens-media` → Settings → Public Access → Custom Domains → Add `media.greenlens.example` → save.
- Update `R2_PUBLIC_BASE_URL=https://media.greenlens.example` trong `.env.production`.

### Step 5 — Cleanup Phase 1 URL (optional)

Tab Public Hostnames của tunnel: xoá entry `<uuid>.cfargotunnel.com` nếu không còn ai dùng. Hoặc giữ làm fallback test.

---

## Verification checklist Phase 2

```bash
# 1) DNS đúng (orange-cloud)
dig api.greenlens.example
# → CNAME tới <tunnel-uuid>.cfargotunnel.com
# Cloudflare IP (104.x, 172.x)

# 2) HTTPS
curl -I https://api.greenlens.example/health
# → HTTP/2 200, Server: cloudflare

# 3) HSTS bật
curl -I https://api.greenlens.example | grep -i strict-transport
# → strict-transport-security: max-age=31536000; includeSubDomains

# 4) WAF active
curl -A "sqlmap" https://api.greenlens.example/api/v1/reports
# → có thể bị Cloudflare block (403)

# 5) Tunnel vẫn healthy
sudo systemctl status cloudflared

# 6) BE không biết public URL — verify
docker compose exec api env | grep -iE "url|host"
# → KHÔNG có api.greenlens.example. BE chỉ biết localhost.
```

## Khi nào cần rollback

Nếu Phase 2 có vấn đề (DNS sai, cert chưa cấp, FE lỗi CORS với domain mới), trong tunnel xoá public hostname `api.greenlens.example` → FE/mobile tạm dùng lại URL Phase 1 cho tới khi fix.

```bash
# Trên CF dashboard → Tunnels → greenlens-vps → Public Hostname → 3-dot menu → Delete
```

DNS record cũng tự xoá theo. URL Phase 1 (`<uuid>.cfargotunnel.com`) vẫn hoạt động vì nó là URL khác trong cùng tunnel.

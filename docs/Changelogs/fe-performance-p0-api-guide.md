# FE Guide — Performance P0 (Rate Limit + Compression)

> **Backend change:** 2026-07-30 · Sprint S1 (P0)  
> **BR:** BR-SYS-004 (global API rate limit), BR-REP-010 (submit quota — không đổi)  
> **Ảnh hưởng:** Tất cả client (Mobile Citizen/Cleaner/Inspector, Web LEO/DEO/Admin)

---

## 1. Tóm tắt thay đổi

| Hạng mục | Trước | Sau |
|----------|-------|-----|
| Global rate limit | Không có | **60 req/phút/IP** (anonymous) · **300 req/phút/user** (JWT) |
| Submit report quota | 5/h + 20/24h (`RATE_LIMIT_EXCEEDED`) | Giữ nguyên — **tách biệt** với global limit |
| Response body | JSON thường | **Brotli/Gzip** nếu client gửi `Accept-Encoding` |
| Production Redis | Optional (in-memory fallback) | **Bắt buộc** — `ConnectionStrings:Redis` (ops/deploy) |

---

## 2. HTTP 429 — Global rate limit

Khi vượt quota **toàn API**, server trả:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 42
Content-Type: application/json
```

```json
{
  "code": "API_RATE_LIMIT_EXCEEDED",
  "message": "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
  "status": 429,
  "data": null
}
```

### Phân biệt 2 loại 429

| Code | Nguồn | Khi nào |
|------|-------|---------|
| `API_RATE_LIMIT_EXCEEDED` | Middleware global (BR-SYS-004) | Bất kỳ endpoint nào vượt 60/300 rpm |
| `RATE_LIMIT_EXCEEDED` | Submit report handler (BR-REP-010) | `POST /reports` vượt 5/h hoặc 20/24h |

**Khuyến nghị FE:** Xử lý chung mọi **429** — đọc `Retry-After` (giây), hiển thị toast/snackbar, backoff trước khi retry. Có thể phân nhánh message theo `code` nếu cần copy UX khác nhau.

### Ví dụ interceptor (axios)

```typescript
api.interceptors.response.use(
  (res) => res,
  async (err) => {
    if (err.response?.status === 429) {
      const retryAfter = Number(err.response.headers['retry-after'] ?? 60);
      const code = err.response.data?.code ?? 'API_RATE_LIMIT_EXCEEDED';
      showRateLimitToast(code, retryAfter);
      // optional: queue retry after retryAfter seconds
    }
    return Promise.reject(err);
  }
);
```

---

## 3. Giới hạn cụ thể

| Actor | Partition key | Limit | Window |
|-------|---------------|-------|--------|
| Anonymous (không JWT) | Client IP (`X-Forwarded-For` nếu qua proxy) | 60 | 1 phút sliding |
| Authenticated (có JWT) | `sub` claim (user Id) | 300 | 1 phút sliding |

> Map pan/zoom nhanh có thể chạm 300/min nếu gọi API liên tục không debounce — **P1** sẽ thêm limit riêng map 20/min (BR-MAP-012). FE nên debounce map refresh ~300–500ms.

---

## 4. Endpoint **không** bị global rate limit

| Path | Lý do |
|------|-------|
| `/health` | Docker / load balancer probe |
| `/swagger`, `/swagger/*` | API docs |
| `/hangfire`, `/hangfire/*` | Job dashboard (admin) |
| `/hubs/notifications` | SignalR WebSocket — long-lived connection |

Các route API controller (`/v1/...`, `/reports`, …) **đều** bị limit.

---

## 5. Response compression

Server hỗ trợ **Brotli** (ưu tiên) và **Gzip**. Không cần đổi code FE nếu HTTP client mặc định gửi:

```http
Accept-Encoding: br, gzip, deflate
```

Fetch/axios trên browser/mobile thường tự thêm header này. Payload JSON nhỏ hơn → tải nhanh hơn trên 3G/4G.

**Lưu ý:** Upload file (presigned URL → R2) **không** qua compression middleware — chỉ response JSON từ API.

---

## 6. Checklist tích hợp FE

- [ ] Global error handler bắt **429** + đọc `Retry-After`
- [ ] Không spam retry ngay lập tức (exponential backoff hoặc đợi `Retry-After`)
- [ ] Map module: debounce bbox requests (chuẩn bị BR-MAP-012 P1)
- [ ] Dashboard admin/analytics: tránh poll < 1s — dễ chạm 300/min khi mở nhiều tab
- [ ] E2E test: mock 429 với body `{ code: "API_RATE_LIMIT_EXCEEDED" }`

---

## 7. Deploy / staging (ops — không code FE)

Production (`appsettings.Production.json`):

```json
"Redis": { "Required": true }
```

Env bắt buộc trên VPS:

```bash
ConnectionStrings__Redis=your-redis-host:6379
```

Nếu thiếu Redis → API **không start** (fail fast). FE sẽ thấy 502/connection refused từ gateway — không phải 429.

---

## 8. Liên kết

| File | Nội dung |
|------|----------|
| [PERFORMANCE_EXECUTION_PRIORITIES.md](../BusinessRule/PERFORMANCE_EXECUTION_PRIORITIES.md) | Roadmap P0–P3 |
| Submit rate limit (citizen) | `fe-citizen-*` guides — `RATE_LIMIT_EXCEEDED` trên POST report |

# API Conventions — SU26SE049 Pollution Reporting System

> **Read this first.** Mọi module spec đều dựa trên các convention này.

---

## 1. Base URL & Versioning

| Environment | Base URL |
|---|---|
| Local | `http://localhost:5000/v1` |
| Dev | `https://api-dev.greenlens.com.vn/v1` |
| Staging | `https://api-stg.greenlens.com.vn/v1` |
| Production | `https://api.greenlens.com.vn/v1` |

**Versioning rule:** Major version trong URL path (`/v1`, `/v2`). Minor changes backward-compatible không bump version.

---

## 2. Standard Response Envelope

**MỌI response (success + error) đều trả về format này:**

```json
{
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "status": 200,
  "data": { ... }
}
```

| Field | Type | Description |
|---|---|---|
| `code` | string | Business code (UPPER_SNAKE_CASE). VD: `SUCCESS`, `INVALID_EMAIL`, `RATE_LIMIT_EXCEEDED` |
| `message` | string | Human-readable message (i18n theo header `Accept-Language`) |
| `status` | integer | HTTP status code (200, 400, 401, 403, 404, 409, 422, 429, 500) |
| `data` | object\|array\|null | Payload chính. `null` khi error hoặc không có data |

### 2.1 Success example

```json
{
  "code": "SUCCESS",
  "message": "Đăng nhập thành công",
  "status": 200,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "eyJhbGc...",
    "user": { "id": "uuid", "email": "a@b.com", "role": "Citizen" }
  }
}
```

### 2.2 Validation error example

```json
{
  "code": "VALIDATION_ERROR",
  "message": "Dữ liệu không hợp lệ",
  "status": 422,
  "data": {
    "errors": [
      { "field": "email", "code": "INVALID_FORMAT", "message": "Email không đúng định dạng" },
      { "field": "password", "code": "TOO_SHORT", "message": "Mật khẩu phải ≥ 8 ký tự" }
    ]
  }
}
```

### 2.3 Paginated list example

```json
{
  "code": "SUCCESS",
  "message": "OK",
  "status": 200,
  "data": {
    "items": [ ... ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 245,
      "totalPages": 13,
      "hasNext": true,
      "hasPrev": false
    }
  }
}
```

---

## 3. Standard HTTP Status Codes

| Status | Khi nào dùng |
|---|---|
| 200 OK | GET, PUT, PATCH thành công |
| 201 Created | POST tạo mới thành công |
| 204 No Content | DELETE thành công, không có body |
| 400 Bad Request | Request format sai (JSON parse fail) |
| 401 Unauthorized | Thiếu/sai token |
| 403 Forbidden | Có token nhưng không đủ quyền |
| 404 Not Found | Resource không tồn tại |
| 409 Conflict | Resource đã tồn tại (email duplicate) |
| 422 Unprocessable Entity | Validation fail (field-level errors) |
| 429 Too Many Requests | Rate limit |
| 500 Internal Server Error | Lỗi server |
| 503 Service Unavailable | AI service / DB down |

---

## 4. Standard Business Codes

### Generic
- `SUCCESS` — thành công
- `VALIDATION_ERROR` — lỗi validation field
- `UNAUTHORIZED` — chưa login
- `FORBIDDEN` — không đủ quyền
- `NOT_FOUND` — không tìm thấy
- `CONFLICT` — duplicate resource
- `RATE_LIMIT_EXCEEDED` — vượt rate limit
- `INTERNAL_ERROR` — lỗi server
- `SERVICE_UNAVAILABLE` — service downstream chết

### Auth-specific (xem chi tiết module Auth)
- `INVALID_CREDENTIALS`, `ACCOUNT_LOCKED`, `EMAIL_NOT_VERIFIED`, `OTP_EXPIRED`, `OTP_INVALID`, `WEAK_PASSWORD`, `EMAIL_TAKEN`, `PHONE_TAKEN`, `TOKEN_EXPIRED`, `INVALID_REFRESH_TOKEN`

### Report-specific
- `INVALID_GPS`, `OUT_OF_VIETNAM`, `IMAGE_TOO_LARGE`, `TOO_MANY_IMAGES`, `INVALID_STATE_TRANSITION`, `DUPLICATE_REPORT`, `REPORT_LOCKED`, `SPAM_DETECTED`

### AI-specific
- `AI_UNAVAILABLE`, `AI_TIMEOUT`, `AI_LOW_CONFIDENCE`

---

## 5. Authentication

### 5.1 Headers chuẩn

```http
Authorization: Bearer {access_token}
Content-Type: application/json
Accept-Language: vi-VN  # or en-US
X-Request-ID: {uuid}    # optional, server tự generate nếu thiếu
```

### 5.2 Token lifecycle

| Token | Lifetime | Storage (client) |
|---|---|---|
| Access Token | 24h | Memory + AsyncStorage (mobile) / httpOnly cookie (web) |
| Refresh Token | 30 days | Secure storage |
| OTP Email | 10 min | Server-side (Redis) |
| OTP SMS | 5 min | Server-side (Redis) |
| Reset password link | 15 min | Server-side (Redis) |

### 5.3 JWT payload chuẩn

```json
{
  "sub": "user-uuid",
  "email": "a@b.com",
  "role": "Citizen",
  "permissions": ["report:create", "report:read"],
  "iat": 1715234567,
  "exp": 1715320967,
  "jti": "token-uuid"
}
```

---

## 6. Field Naming Conventions

| Type | Convention | Example |
|---|---|---|
| JSON keys | `camelCase` | `firstName`, `createdAt` |
| URL paths | `kebab-case` | `/pollution-reports`, `/cleanup-tasks` |
| Query params | `camelCase` | `?pageSize=20&sortBy=createdAt` |
| Headers | `Kebab-Case` or `X-Prefix` | `Authorization`, `X-Request-ID` |
| DB columns | `snake_case` | `first_name`, `created_at` |
| Enum values | `UPPER_SNAKE_CASE` | `IN_PROGRESS`, `RESOLVED` |
| ID format | `UUID v4` | `550e8400-e29b-41d4-a716-446655440000` |
| Timestamp | ISO 8601 UTC | `2026-05-09T10:15:30Z` |
| Date | `YYYY-MM-DD` | `2026-05-09` |
| Coordinates | decimal degrees | `lat: 10.7626, lng: 106.6602` |

---

## 7. Pagination, Sorting, Filtering

### Query params chuẩn

```
GET /v1/reports?page=1&pageSize=20&sortBy=createdAt&sortOrder=desc&status=SUBMITTED&type=TRASH
```

| Param | Default | Max | Description |
|---|---|---|---|
| `page` | 1 | - | 1-indexed |
| `pageSize` | 20 | 100 | Items per page |
| `sortBy` | `createdAt` | - | Field name (camelCase) |
| `sortOrder` | `desc` | - | `asc` / `desc` |

Filtering: query param có cùng tên field, support multiple values bằng comma: `?status=SUBMITTED,VERIFIED`.

---

## 8. File Upload Convention

**Endpoint pattern:** `POST /v1/{resource}/{id}/media` hoặc `POST /v1/uploads`

**Headers:**
```http
Content-Type: multipart/form-data
Authorization: Bearer {token}
```

**Form fields:**
- `files[]`: binary (max 5 files)
- `type`: `IMAGE` | `VIDEO` | `BEFORE` | `AFTER`
- `metadata`: optional JSON string

**Response:**
```json
{
  "code": "SUCCESS",
  "status": 201,
  "message": "Upload thành công",
  "data": {
    "uploaded": [
      {
        "id": "uuid",
        "url": "https://cdn.../abc.jpg",
        "thumbnailUrl": "https://cdn.../abc_thumb.jpg",
        "type": "IMAGE",
        "size": 2048576,
        "mimeType": "image/jpeg",
        "exif": { "gps": { "lat": 10.76, "lng": 106.66 }, "takenAt": "..." }
      }
    ]
  }
}
```

**File rules (BR-REP-001, 002):**
- Image: jpg/jpeg/png/webp, ≤10MB, 1–5 files
- Video: mp4/mov, ≤100MB, max 1 file

---

## 9. Rate Limiting

**Headers trả về kèm mọi response:**
```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1715234600
Retry-After: 30  # chỉ khi 429
```

| Scope | Limit |
|---|---|
| Anonymous public API | 60 req/min/IP |
| Authenticated user | 300 req/min/user |
| Submit report | 5/h, 20/24h per Citizen (BR-REP-010) |
| Refresh map data | 20/min/user (BR-MAP-012) |
| Login attempts | 5 fail/15min → lock 30min (BR-AUTH-011) |

---

## 10. Audit Log Convention

Mọi action nhạy cảm phải log (BR-ADM-010):

```json
{
  "id": "uuid",
  "actorId": "user-uuid",
  "actorRole": "Officer",
  "action": "REPORT_VERIFIED",
  "targetType": "Report",
  "targetId": "report-uuid",
  "metadata": { "previousStatus": "SUBMITTED", "newStatus": "VERIFIED" },
  "ip": "192.168.1.1",
  "userAgent": "Mozilla/5.0...",
  "timestamp": "2026-05-09T10:15:30Z"
}
```

**Retention:** 12 months minimum.

---

## 11. Common Enums

### UserRole
```
CITIZEN | OFFICER | CLEANUP_TEAM | ADMIN
```

### ReportStatus
```
SUBMITTED | VERIFIED | IN_PROGRESS | RESOLVED | CLOSED | REJECTED | DUPLICATE
```

### PollutionType
```
TRASH | WASTEWATER | CHEMICAL | OTHER
```

### Severity
```
LOW | MEDIUM | HIGH | CRITICAL
```

### NotificationChannel
```
PUSH | EMAIL
```

---

## 12. Definition of Done — API Task

Mỗi API task xem là DONE khi:

- [ ] Endpoint code merged vào `develop`
- [ ] Request/response match spec 100%
- [ ] Response envelope `{code, message, status, data}` đúng convention
- [ ] Validation đầy đủ (field-level errors)
- [ ] Authorization check (role + ownership)
- [ ] Audit log cho action nhạy cảm
- [ ] Unit test happy path + ≥1 error case
- [ ] Integration test với DB
- [ ] Swagger/OpenAPI annotation đầy đủ
- [ ] Postman collection updated
- [ ] BR IDs map vào commit message
- [ ] PR review ≥ 1 người approve
- [ ] QA sign-off

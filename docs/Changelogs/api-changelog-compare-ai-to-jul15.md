# API Changelog — Compare Image AI → 2026-07-15

> **Branch:** `feature/duplicate-ai-compare-image` (chưa merge `develop`)  
> **Phạm vi:** Tất cả API mới / thay đổi contract kể từ khi bắt đầu duplicate detection (BR-REP-030..033) đến hiện tại.

Tài liệu chi tiết từng module:

| Module | Doc |
|--------|-----|
| Duplicate (LEO) | [`fe-leo-duplicate-detection-guide.md`](./fe-leo-duplicate-detection-guide.md) |
| Comments | [`fe-comments-api-guide.md`](./fe-comments-api-guide.md) |
| Blocked words (Admin) | [`api-admin-blocked-words.md`](./api-admin-blocked-words.md) |

---

## 1. Tóm tắt theo phiên

| Phiên / chủ đề | BR | API / thay đổi chính |
|----------------|-----|----------------------|
| Duplicate detection | BR-REP-030..033 | 4 endpoint Reports + submit response fields |
| Comments | BR-CMT-001..004 | CommentsController + upload ảnh comment |
| Notifications | BR-REP-033, BR-NTF-* | Template `duplicate_review_needed` (seed) |
| AiRetryJob | BR-AI-006 | Không có HTTP — job nền 5 phút |
| Report submit hardening | BR-REP-004, 010, 011 | Submit validation + 429 + EXIF fields |
| Admin blocked words | BR-REP-004, BR-CMT-003, BR-ADM-010 | 4 endpoint Admin CRUD |

---

## 2. API mới — Reports (`ReportsController`)

**Base:** `/v1/reports` · **Auth:** Bearer (role tùy endpoint)

| Method | Path | Role | BR | Mô tả |
|--------|------|------|-----|-------|
| GET | `/duplicate-candidates` | LEO, DEO, Admin | BR-REP-031 | Queue báo cáo `isPossibleDuplicate = true` |
| POST | `/{id}/confirm-duplicate` | LEO, DEO, Admin | BR-REP-032 | Gộp vào báo cáo gốc, merge media + comments |
| POST | `/{id}/dismiss-duplicate` | LEO, DEO, Admin | BR-REP-031 | Bác bỏ cờ trùng |
| POST | `/{id}/flag` | Citizen | BR-REP-033 | Gắn cờ spam/duplicate/…; ≥3 flag → notify LEO |

### `POST /{id}/confirm-duplicate`

**Body:** `{ "primaryReportId": "uuid" }`  
**Response 200:** merge result (duplicate report id, primary id, …)

### `POST /{id}/flag`

**Body:** `{ "flagType": "Duplicate|Invalid|Spam|Inappropriate", "reason": "optional" }`

---

## 3. Thay đổi — `POST /v1/reports` (Submit báo cáo)

### Response `data` — field mới

| Field | Type | BR | Mô tả |
|-------|------|-----|-------|
| `isPossibleDuplicate` | bool | BR-REP-030 | Tier 1 geo phát hiện trùng tiềm năng |
| `possibleDuplicateOfReportId` | Guid? | BR-REP-030 | ID báo cáo gốc nghi ngờ |
| `isSuspicious` | bool | BR-REP-011 | EXIF thiếu hoặc timestamp cũ >1h |
| `exifWarning` | string? | BR-REP-011 | `"Ảnh có thể không phản ánh hiện trạng thực tế"` |

### Lỗi mới

| Code | HTTP | BR | Khi nào |
|------|------|-----|---------|
| `RATE_LIMIT_EXCEEDED` | 429 | BR-REP-010 | >5 báo cáo/giờ hoặc >20/24h |
| `INAPPROPRIATE_CONTENT` | 400 | BR-REP-004 | Mô tả chứa từ bị chặn |
| `DESCRIPTION_TOO_SHORT` | 400 | BR-REP-004 | Mô tả có nhưng <10 ký tự |

### Validation mô tả (BR-REP-004)

- Không bắt buộc; nếu có: **10–1000** ký tự + word filter.

---

## 4. API mới — Comments (`CommentsController`)

**Base:** `/v1` · **Tag Swagger:** `💬 Comments`

| Method | Path | Auth | BR |
|--------|------|------|-----|
| GET | `/reports/{reportId}/comments` | Bearer | BR-CMT-001 |
| POST | `/reports/{reportId}/comments` | Bearer | BR-CMT-001..003 |
| PUT | `/comments/{commentId}` | Bearer (author) | BR-CMT-002 |
| DELETE | `/comments/{commentId}` | Bearer (author) | BR-CMT-002 |
| POST | `/comments/{commentId}/hide` | LEO, DEO, Admin | BR-CMT-004 |

### Media

| Method | Path | Mô tả |
|--------|------|-------|
| POST | `/v1/media/comments/images` | Upload ảnh comment (max 5MB, `MediaController`) |

### Lỗi comment quan trọng

| Code | HTTP | Mô tả |
|------|------|-------|
| `INAPPROPRIATE_CONTENT` | 422 | Word filter — 3 lần → `COMMENT_BANNED` 7 ngày |
| `COMMENT_NOT_ALLOWED` | 403 | Báo cáo ẩn tên (BR-REP-012) |

---

## 5. API mới — Admin Blocked Words

**Base:** `/v1/admin/blocked-words` · **Role:** `Admin` only

| Method | Path | Mô tả |
|--------|------|-------|
| GET | `/blocked-words` | Danh sách (page, search, isActive) |
| POST | `/blocked-words` | Thêm từ |
| PUT | `/blocked-words/{id}` | Sửa / bật lại |
| DELETE | `/blocked-words/{id}` | Vô hiệu hóa (soft) |

Chi tiết: [`api-admin-blocked-words.md`](./api-admin-blocked-words.md)

---

## 6. Thay đổi không phải HTTP

| Thành phần | BR | Mô tả |
|------------|-----|-------|
| `CompareDuplicateImagesJob` | BR-REP-030, BR-AI-002 | Hangfire on-demand sau Tier 1; gọi Python `/api/v1/compare-images` |
| `AiRetryJob` | BR-AI-006 | Mỗi 5 phút, retry `ai_pending` trong 1h |
| `DuplicateReviewNeeded` template | BR-REP-033 | Seed notification khi ≥3 flag |
| `ProfanityFilter` | BR-REP-004, BR-CMT-003 | Đọc `blocked_words` DB + cache (thay appsettings) |
| `RedisReportSubmissionRateLimiter` | BR-REP-010 | Redis sorted set; fallback in-memory nếu không có Redis |
| `MetadataExtractorImageExifAnalyzer` | BR-REP-011 | EXIF trên ảnh chính khi submit |

---

## 7. Domain / DB schema mới

| Migration | Nội dung |
|-----------|----------|
| `20260713145829_*` | `penalty_payments` soft-delete |
| `20260713145907_*` | Reports: duplicate detection columns + indexes |
| `20260714184414_AddCommentModule` | `comments`, `comment_media`, user ban fields, `reports.hide_reporter_name` |
| `202607150210_AddBlockedWords` | `blocked_words` + seed 10 từ |

**Report fields mới (duplicate):** `is_possible_duplicate`, `possible_duplicate_of_report_id`, `duplicate_detection_source`, `ai_similarity_score`

**Report fields có sẵn dùng cho BR-REP-011:** `is_suspicious`, `suspicious_reasons`  
**ReportMedia:** `exif_data` (jsonb) populated on submit

---

## 8. ErrorType mới

| `ErrorType` | HTTP | Dùng cho |
|-------------|------|----------|
| `RateLimited` | 429 | BR-REP-010 submit quota |

---

## 9. Cấu hình mới

| Key | Mô tả |
|-----|-------|
| `ConnectionStrings:Redis` | Rate limit BR-REP-010 (placeholder `localhost:6379`) |
| `Ai:BaseUrl` | Python AI service (compare-images + classify) |

**Không còn dùng** `Moderation:BlockedWords` trong appsettings — chuyển sang Admin CRUD + DB.

---

## 10. Checklist tích hợp FE

### Citizen app

- [ ] Submit: handle `isPossibleDuplicate`, `exifWarning`, 429 rate limit
- [ ] Report detail: tab Comments + upload ảnh
- [ ] Flag report (`POST /reports/{id}/flag`)

### LEO dashboard

- [ ] Queue duplicate: `GET /reports/duplicate-candidates`
- [ ] Confirm / dismiss duplicate
- [ ] Hide comment vi phạm

### Admin dashboard

- [ ] CRUD blocked words (`/admin/blocked-words`)
- [ ] (Có sẵn) Hide/unhide report, notification templates, gamification config

---

## 11. Lệnh deploy / migration

```powershell
dotnet ef database update --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api
```

Python AI (Tier 2): deploy endpoint theo `docs/ImageCompareAi/ai-compare-images-spec.md`.

---

*Cập nhật: 2026-07-15*

# Plan — Luồng tạo báo cáo, gọi FastAPI detect ảnh, và Officer xác nhận
> **Mục tiêu:** Hoàn thiện flow **User gửi báo cáo (ảnh + GPS)** → **BE .NET gọi FastAPI detect** (hiện có thể để trống/stub) → **phản hồi cho user** → **Officer verify/accept**.  
> **Nguyên tắc:** Clean Architecture — Application định nghĩa interface; Infrastructure gọi HTTP FastAPI; **không** hardcode URL trong handler; **scale** sau bằng cách thay implementation + config, không đổi handler.
**Phiên bản:** 1.0  
**Đồng bộ convention:** `CLAUDE.md`, `00_API_CONVENTIONS.md`, envelope `{ code, message, status, data }`.
---
## 1. Tóm tắt luồng người dùng & nghiệp vụ
[Citizen] │ ├─► Upload ảnh (presigned S3 / multipart — theo spec đồ án) │ ├─► POST tạo báo cáo (lat, lng, category, mediaIds, …) │ ▼ [GreenLens API .NET] │ ├─► (Tuỳ chọn / sau này bật) Gọi FastAPI detect ảnh │ └── GET/POST HTTP tới service đã train (hoặc stub trả fixed JSON) │ ├─► Validate business (GPS VN, duplicate PostGIS, rate limit, …) │ ├─► Lưu Report (Submitted) │ └─► Response cho user: thành công / lỗi validation / gợi ý từ AI (khi có)

[Officer] │ ├─► GET danh sách / queue báo cáo Submitted (filter, SLA) │ └─► POST verify / reject / duplicate → state machine (Verified | Rejected | Duplicate)


**Hiện trạng bạn chọn:** Model chưa xong → **không bắt buộc** chặn user chờ AI; có thể **tắt gọi** hoặc **stub** FastAPI trả payload rỗng / success giả để luồng **User → Officer** chạy end-to-end.
---
## 2. Kiến trúc tích hợp FastAPI (scale được)
### 2.1 Tách lớp (bắt buộc để sau không refactor handler)
| Layer | Trách nhiệm |
|-------|-------------|
| **Application** | `IImageDetectionService` (hoặc `IAiReportImageService`) — interface + DTO request/response thuần, **không** `HttpClient`. |
| **Infrastructure** | `FastApiImageDetectionClient` — `HttpClient` gọi FastAPI, timeout, retry nhẹ, logging; map JSON → DTO. |
| **Api / Options** | `AiDetectionOptions`: `BaseUrl`, `ApiKey` (user-secrets), `TimeoutSeconds`, **`Enabled`** (feature flag). |
### 2.2 Khi `Enabled = false` hoặc chưa cấu hình URL
- Implement **`NullImageDetectionService`** hoặc trong client: trả ngay `DetectionSkipped` / empty labels **không** fail request tạo báo cáo.
- Handler submit report **không** throw vì “AI down” — trừ khi sau này product quyết là **bắt buộc**.
### 2.3 Hợp đồng HTTP với FastAPI (chốt sớm 1 trang OpenAPI)
Gợi ý minimal:
- **Request:** `reportId` hoặc `mediaUrl` / `imageBase64` (khuyến nghị URL đã upload để FastAPI pull — tránh body khổng lồ).
- **Response:** `{ "labels": [...], "confidence": 0.0-1.0, "isPollutionRelated": bool, "raw": {} }` — BE chỉ map vào value object.
### 2.4 An toàn & BR
- **BR-AI-007:** Strip GPS nhạy cảm trong metadata gửi sang AI nếu có EXIF.
- **Secrets:** URL + key FastAPI trong user-secrets / env — không commit.
---
## 3. Luồng chi tiết: Tạo báo cáo → (AI) → Phản hồi user
| Bước | Việc | Ghi chú |
|------|------|---------|
| 1 | Client upload ảnh | Presigned hoặc multipart theo spec |
| 2 | Client `POST /v1/pollution-reports` | Body: refs ảnh, lat/lng, type, … |
| 3 | Validator | FluentValidation — BR-REP-001..005, 010 |
| 4 | **Nếu AI bật:** gọi `IImageDetectionService` | Timeout ngắn (BR-AI-006); fail → `ai_pending` hoặc bỏ qua |
| 5 | Duplicate check PostGIS | BR-REP-030 |
| 6 | `Report.Create(...)` → `Submitted` | BR-REP-013 |
| 7 | `SaveChanges` + outbox | |
| 8 | Response envelope | `reportId`; có thể kèm `aiHints` nullable |
**Hiện trạng “để trống”:** Bước 4 dùng **Null/stub** — không gợi ý AI hoặc `"skipped": true`.
---
## 4. Luồng Officer: tiếp nhận → chấp nhận báo cáo
| Bước | Việc | BR gợi ý |
|------|------|---------|
| 1 | Officer đăng nhập + policy `CanVerifyReport` | BR-OFF-004 |
| 2 | `GET` queue `Submitted` (pagination) | BR-OFF-002 |
| 3 | `GET` chi tiết | |
| 4 | `POST` verify → `report.Verify(officerId)` | BR-REP-020 |
| 5 | Audit log | BR-ADM-010 |
| 6 | Notify citizen (async) | BR-NTF-* |
---
## 5. Checklist triển khai
### Phase 0 — Contract & cờ
- [ ] OpenAPI FastAPI tối thiểu.
- [ ] `AiDetection:Enabled`, `AiDetection:BaseUrl` (secrets ngoài repo).
- [ ] `IImageDetectionService` + DTO trong Application.
### Phase 1 — Submit không AI bắt buộc
- [ ] Entity `Report` + migration PostGIS.
- [ ] `SubmitReportCommand` + validator + handler (BR trong XML).
- [ ] **NullImageDetectionService** khi `Enabled = false`.
- [ ] Test happy path + GPS invalid.
### Phase 2 — Gắn FastAPI
- [ ] `FastApiImageDetectionClient` + timeout.
- [ ] Lỗi network không fail submit (mặc định capstone).
- [ ] Optional: cột JSON `ai_summary` / bảng `report_ai_results`.
### Phase 3 — Officer
- [ ] List Submitted + pagination.
- [ ] `VerifyReportCommand` + audit + test.
### Phase 4 — Scale
- [ ] Hangfire: AI sau `SaveChanges`.
- [ ] Rate limit / cache map (BR-MAP-012).
---
## 6. ADR ngắn
| Chủ đề | Đề xuất |
|--------|---------|
| AI chặn submit? | **Không** khi đang train. |
| AI timeout? | Soft fail; Officer xử lý. |
| Sync vs async? | MVP stub sync; sau: job async. |
---
## 7. Gợi ý path code sau này
| Khu vực | Path |
|---------|------|
| Interface | `Application/Common/Interfaces/IAi/IImageDetectionService.cs` |
| Client | `Infrastructure/Ai/FastApiImageDetectionClient.cs` |
| Stub | `Infrastructure/Ai/NullImageDetectionService.cs` |
| Reports | `Application/Features/Reports/SubmitReport/`, `VerifyReport/` |
---
**Kết luận:** Ship **Create → Officer** sớm; FastAPI chỉ là **adapter** — stub khi chưa có model; bật config + client khi sẵn sàng.
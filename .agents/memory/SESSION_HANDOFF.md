# Session Handoff — GreenLens Backend

> **Cập nhật lần cuối:** 2026-06-29 14:30 · **Phiên bản:** 9 · **Agent:** Antigravity

## 0. TL;DR
Backend .NET 9 GreenLens. Phiên vừa rồi đã implement **Notification module** (BR-NTF-001..004), **P0 Blocking** (TransactionBehavior + 3 SLA background jobs), và API **GET /v1/companies/my-ward** cho LEO xem công ty phục vụ phường/xã. Tất cả đã commit & merge vào `develop`. Branch sạch, không có uncommitted changes.

## 1. Mục tiêu & Bối cảnh
- **Mục tiêu tổng thể:** Backend .NET 9 cho ứng dụng báo cáo ô nhiễm môi trường (SU26SE049)
- **Phạm vi phiên 9:** P0 Blocking (Transaction + SLA Jobs), Notification module, LEO company dispatch API
- **Ngôn ngữ:** Tiếng Việt (giao tiếp + XML doc BR), English (code)

## 2. Quyết định đã chốt (Locked Decisions)

| # | Quyết định | Lý do | Ngày |
|---|---|---|---|
| 1 | UserPoints tách khỏi User entity | SRP: gamification toggle độc lập | 2026-06-26 |
| 2 | Decoupled via DomainEvent (MediatR INotification) | Zero changes to existing handlers | 2026-06-26 |
| 3 | Badge "Verified Citizen" → bỏ qua (chờ KYC) | Chờ KYC module | 2026-06-26 |
| 4 | Badge "Hotspot Hunter" → seed nhưng chưa auto-award | Chờ BR-MAP-010 | 2026-06-26 |
| 5 | LeaderboardSnapshotJob bằng Hangfire | User yêu cầu trực tiếp | 2026-06-26 |
| 6 | API Docs v1.7 là "Source of Truth" cho endpoint cũ | Từ phiên trước | 2026-06-17 |
| 7 | `contractType` gửi numeric enum (0=Subsidiary, 1=Bidding) | Từ phiên trước | 2026-06-17 |
| 8 | AGENTS.md "Senior Dev" mindset rules | Tránh over-engineering | 2026-06-26 |
| 9 | Branch/commit naming: KHÔNG dùng mã BR/P0 | User yêu cầu tên mô tả rõ ràng | 2026-06-28 |
| 10 | Notification list dùng pagination chuẩn (page, pageSize, totalCount) | Giống các API list khác | 2026-06-28 |
| 11 | `GET /v1/companies/my-ward` thay vì `GET /v1/reports/{id}/dispatchable-companies` | LEO cần xem công ty trên dashboard mà không cần reportId cụ thể | 2026-06-28 |

## 3. Trạng thái hiện tại

### ✅ Đã hoàn thành (phiên 9 — 2026-06-28)

**P0 Blocking:**
- `TransactionBehavior` — MediatR pipeline wrap Command handlers trong DB transaction
- `AutoCloseResolvedReportJob` — Hangfire hourly, tự đóng report đã Resolved ≥7 ngày (BR-REP-016, BR-REP-025)
- `SlaBreachVerificationJob` — Hangfire every 15', phát hiện report Submitted quá hạn xác minh
- `SlaBreachResolutionJob` — Hangfire every 30', phát hiện report InProgress quá hạn xử lý (BR-OFF-020)

**Notification Module (BR-NTF-001..004):**
- Domain: `Notification`, `NotificationPreference` entities
- Application: `NotificationService` (anti-spam, preferences check, dispatch) + event handlers
- Infrastructure: `FcmPushNotificationSender`, `IEmailSender` template-based
- API: `NotificationsController` — 6 endpoints:
  - `GET /v1/notifications` (paginated list)
  - `POST /v1/notifications/{id}/read` (đánh dấu đã đọc)
  - `POST /v1/notifications/read-all` (đánh dấu tất cả đã đọc)
  - `GET /v1/notifications/preferences`
  - `PUT /v1/notifications/preferences`
  - `POST /v1/notifications/device-token`
- Docs: `docs/Notification/NotificationModule.md`
- `br_v12_comparison_report.md` cập nhật status

**LEO Company Dispatch API:**
- `GET /v1/companies/my-ward` — LEO xem công ty Active phục vụ phường/xã mình (tự resolve từ ICurrentUser)
- Feature slice: `Application/Features/Organization/GetOfficeCompanies/`
- Endpoint trên `CompaniesController`, Tag "📌 LEO Dashboard"

### ✅ Đã hoàn thành (phiên trước)
- Gamification module (BR-GAM-001..006): 23 files, 11 unit tests
- DomainEvent infrastructure (UnitOfWork dispatch)
- Hangfire setup (AspNetCore + PostgreSql)
- API Documentation v1.7 (106 endpoints)
- E2E test Company Management (20/20 PASS)
- AGENTS.md "Senior Dev" rules + BR v1.2 sync

### ⚠️ Deferred / Chưa làm
- Badge `hotspot_hunter` auto-award (chờ BR-MAP-010)
- Badge `streak_7d` auto-award (cần consecutive-day tracking)
- Badge `verified_citizen` (chờ KYC module)
- BR-GAM-002 Anonymous opt-out (chờ Privacy settings)
- Hangfire dashboard production auth filter
- Leaderboard materialized cache table (hiện tính on-the-fly)

## 4. Việc tiếp theo (Next Steps)
- [ ] Implement Comments module (BR-CMT-001..004) — chưa có gì
- [ ] AI Service: BR-AI-006 fallback retry job
- [ ] Administration: BR-ADM-004..008, 010, 012 (thiếu nhiều)
- [ ] Data Privacy: BR-DAT-002..005
- [ ] Login-level blocking cho deactivated staff
- [ ] Map module: BR-MAP-001..012 (heatmap, hotspot, nearby)
- [ ] Cập nhật API Documentation lên v1.8+ (thêm Gamification, Notification, company my-ward)

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|---|---|---|
| `docs/BusinessRule/br_v12_comparison_report.md` | So sánh BR v1.2 vs hệ thống | ✅ Đã cập nhật (P0 + NTF) |
| `docs/Notification/NotificationModule.md` | API & architecture guide Notification | ✅ Mới tạo |
| `docs/gamification-module.md` | API & architecture guide Gamification | ✅ Ổn định |
| `docs/API_DASHBOARD_AND_FLOW.md` | API docs tổng (v1.7) | Cần cập nhật thêm endpoints mới |
| `.agents/AGENTS.md` | Rules cho agent | ✅ Ổn định |
| `OVERVIEW.md` | Project overview + conventions | ✅ Sync BR v1.2 |
| `src/Greenlens.Application/Common/Behaviors/TransactionBehavior.cs` | MediatR pipeline transaction | ✅ Mới |
| `src/Greenlens.Infrastructure/BackgroundJobs/AutoCloseResolvedReportJob.cs` | Hangfire — auto close | ✅ Mới |
| `src/Greenlens.Infrastructure/BackgroundJobs/SlaBreachVerificationJob.cs` | Hangfire — SLA verify | ✅ Mới |
| `src/Greenlens.Infrastructure/BackgroundJobs/SlaBreachResolutionJob.cs` | Hangfire — SLA resolve | ✅ Mới |
| `src/Greenlens.Api/Controllers/NotificationsController.cs` | 6 notification endpoints | ✅ Mới |
| `src/Greenlens.Api/Controllers/CompaniesController.cs` | +my-ward endpoint | ✅ Sửa |
| `src/Greenlens.Application/Features/Organization/GetOfficeCompanies/` | LEO company query | ✅ Mới |

## 6. Kiến thức nền & Quy ước
- **Tech stack:** .NET 9, ASP.NET Core, EF Core 9, PostgreSQL + PostGIS, Hangfire, MediatR, FluentValidation, Mapster
- **Architecture:** Clean Architecture (Domain → Application → Infrastructure ← Api)
- **CQRS:** MediatR Command/Query per vertical slice
- **DomainEvent flow:** Entity.AddDomainEvent → UnitOfWork.SaveChanges → MediatR.Publish after commit
- **Naming:** snake_case DB (UseSnakeCaseNamingConvention), PascalCase C#
- **HasFilter trong EF:** Dùng `"column_name"` (snake_case), KHÔNG `"PropertyName"`
- **Build:** `dotnet build -v q` | **Test:** `dotnet test --no-build`
- **Migration:** `dotnet ef migrations add <Name> --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api --output-dir Persistence/Migrations`
- **Git:** Conventional Commits, branch `feature/<slug>`, KHÔNG dùng mã BR/P0 trong tên
- **LEO dispatch flow:** LEO verify → `GET /v1/companies/my-ward` → chọn công ty → `POST /v1/reports/{id}/dispatch-to-company` → CM phân team
- **Company ↔ Ward:** `CompanyServiceArea.WardCode` link tới `LocalOffice.WardCode` (không dùng LocalOfficeId)

## 7. Câu hỏi mở / Cần xác nhận
- Module tiếp theo để implement? (Comments? AI retry? Map? Admin?)

## 8. Thuật ngữ
| Thuật ngữ | Nghĩa |
|---|---|
| LEO | Local Environmental Officer (cán bộ MT xã/phường) |
| DEO | Department Environmental Officer (cán bộ MT sở) |
| CM | Company Manager (quản lý công ty DVMT) |
| BR-NTF-xxx | Business Rule — Notification module |
| BR-CMP-xxx | Business Rule — Company module |
| SLA | Service Level Agreement — thời hạn xử lý report theo severity |

## 9. Change Log
- 2026-06-17 — API Documentation v1.7 + E2E test Company Management (20/20 PASS)
- 2026-06-26 — AGENTS.md "Senior Dev" rules + BR v1.2 sync + OVERVIEW.md update
- 2026-06-26 — Full Gamification module (BR-GAM-001..006): 23 new files, 77/77 tests pass, Hangfire setup
- 2026-06-28 — P0 Blocking (TransactionBehavior + 3 SLA jobs) + Notification module (6 endpoints) + docs
- 2026-06-28 — LEO company dispatch: `GET /v1/companies/my-ward` (resolve office by ICurrentUser)

# Session Handoff — GreenLens Backend

> **Cập nhật lần cuối:** 2026-06-30 16:47 · **Phiên bản:** 10 · **Agent:** Antigravity

## 0. TL;DR
Backend .NET 9 GreenLens. Phiên 10 đã implement **BR-AUTH** batch (009, 011, 012, 015, 020, 021) và **BR-ORG** batch (014, 015, 016, 021) bao gồm: invitation flow thay thế instant recruit, reject re-queue, LEO manual escalate to DEO, và release staff. Tất cả build ✅ 0 errors. **Chưa commit** — cần tạo branch `feature/invitation-flow-and-report-escalation` và commit.

## 1. Mục tiêu & Bối cảnh
- **Mục tiêu tổng thể:** Backend .NET 9 cho ứng dụng báo cáo ô nhiễm môi trường (SU26SE049)
- **Phạm vi phiên 10:** BR-AUTH batch (role assignment, lockout, password history, ban, restore) + BR-ORG batch (SLA escalation, reject re-queue, city-level route, invitation system)
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
| 10 | Notification list dùng pagination chuẩn | Giống các API list khác | 2026-06-28 |
| 11 | `GET /v1/companies/my-ward` thay vì per-report | LEO dashboard không cần reportId | 2026-06-28 |
| 12 | BR-AUTH-015: dùng `IsBanned` bool + toggle API | Đơn giản, không cần ban history | 2026-06-29 |
| 13 | BR-AUTH-021: RestoreAccount riêng biệt | Tách khỏi DeleteUser flow | 2026-06-29 |
| 14 | BR-AUTH-009: Tách riêng, không gộp vào AdminController | Mỗi role-assignment có validation riêng | 2026-06-29 |
| 15 | BR-ORG-015: LEO reject → status giữ Submitted, clear AssignedOfficeId → re-queue | Không dùng terminal Rejected status | 2026-06-29 |
| 16 | BR-ORG-016: **LEO manual escalate** thay vì flag `IsCityLevelRoute` trên Ward | Flag trên Ward không chính xác — 1 phường có cả tuyến cấp TP lẫn hẻm. LEO nhìn ảnh + GPS → quyết định escalate | 2026-06-30 |
| 17 | BR-ORG-021: Invitation flow (7 ngày) + ReleaseStaff | Không instant role change — citizen phải accept. Release = undo sai | 2026-06-29 |

## 3. Trạng thái hiện tại

### ✅ Đã hoàn thành (phiên 10 — 2026-06-29/30)

**BR-AUTH batch:**
- BR-AUTH-009: `AssignRoleCommand` (Admin only, validate role assignment rules)
- BR-AUTH-011: Account lockout (5 fails / 15min → lock 30min, lưu `FailedLoginCount` + `LockoutEnd`)
- BR-AUTH-012: Auto-unlock check trong login flow
- BR-AUTH-015: `IsBanned` flag + `ToggleBanCommand` + login check
- BR-AUTH-020: `PasswordHistory` entity (3 gần nhất), check trong ChangePassword
- BR-AUTH-021: `RestoreAccountCommand` (undo soft delete trong 90 ngày)

**BR-ORG batch:**
- BR-ORG-014: SLA escalation — `SlaBreachVerificationJob` gọi `Report.EscalateToDepartment()` khi breach
- BR-ORG-015: Reject re-queue — `RejectReportCommandHandler` giữ Submitted, clear office → Department queue
- BR-ORG-016: ~~IsCityLevelRoute flag~~ → **LEO manual escalate** (`POST /v1/reports/{id}/escalate`)
- BR-ORG-021: Invitation flow:
  - `StaffInvitation` entity (7d expiry, Accept/Decline/Cancel)
  - `RecruitStaff` → tạo invitation thay vì instant role change
  - `AcceptInvitation` → role change + assign office/team
  - `DeclineInvitation` → giữ Citizen
  - `GetMyInvitations` → Citizen xem danh sách
  - `ReleaseStaff` → revert role → Citizen, clear office, remove teams

**New controllers & endpoints:**
- `InvitationsController` — 3 endpoints (GET my, POST accept, POST decline)
- `ReportsController` — +1 endpoint (POST escalate)
- `LocalOfficesController` — +1 endpoint (DELETE release staff), updated RecruitStaff swagger
- `DepartmentsController` — removed ToggleCityLevelRoute (replaced by manual escalate)

**Documentation:**
- `docs/api-invitation-escalation-guide.md` — FE integration guide
- `docs/BusinessRule/br_v12_comparison_report.md` — cập nhật BR-ORG-014/015/016/020/021

### ✅ Đã hoàn thành (phiên trước)
- Gamification module (BR-GAM-001..006): 23 files, 11 unit tests
- P0 Blocking (TransactionBehavior + 3 SLA jobs)
- Notification module (BR-NTF-001..004): 6 endpoints
- LEO Company Dispatch API (`GET /v1/companies/my-ward`)
- DomainEvent infrastructure, Hangfire setup
- API Documentation v1.7, E2E tests

### ⚠️ Deferred / Chưa làm
- Badge `hotspot_hunter` auto-award (chờ BR-MAP-010)
- Badge `streak_7d` auto-award (cần consecutive-day tracking)
- Badge `verified_citizen` (chờ KYC module)
- BR-GAM-002 Anonymous opt-out (chờ Privacy settings)
- Hangfire dashboard production auth filter
- Leaderboard materialized cache table

## 4. Việc tiếp theo (Next Steps)
- [ ] **Commit phiên 10**: branch `feature/invitation-flow-and-report-escalation`
- [ ] Comments module (BR-CMT-001..004)
- [ ] AI Service: BR-AI-006 fallback retry job
- [ ] Administration: BR-ADM-004..008, 010, 012
- [ ] Data Privacy: BR-DAT-002..005
- [ ] Map module: BR-MAP-001..012 (heatmap, hotspot, nearby)
- [ ] Cập nhật API Documentation lên v1.8+
- [ ] Unit tests cho invitation flow, escalate, reject re-queue

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|---|---|---|
| `docs/BusinessRule/br_v12_comparison_report.md` | So sánh BR v1.2 vs hệ thống | ✅ Cập nhật ORG batch |
| `docs/api-invitation-escalation-guide.md` | FE guide invitation + escalation | ✅ Mới tạo |
| `src/Greenlens.Domain/Entities/StaffInvitation.cs` | Invitation entity (7d, accept/decline/cancel) | ✅ Mới |
| `src/Greenlens.Domain/Entities/PasswordHistory.cs` | Password history (3 gần nhất) | ✅ Mới |
| `src/Greenlens.Domain/Entities/User.cs` | +ClearOfficeAssignment, Ban/Unban | ✅ Sửa |
| `src/Greenlens.Domain/Entities/Report.cs` | +EscalateToDepartment, Reject re-queue | ✅ Sửa |
| `src/Greenlens.Application/Features/Organization/AcceptInvitation/` | Accept invitation slice | ✅ Mới |
| `src/Greenlens.Application/Features/Organization/DeclineInvitation/` | Decline invitation slice | ✅ Mới |
| `src/Greenlens.Application/Features/Organization/GetMyInvitations/` | Get invitations query | ✅ Mới |
| `src/Greenlens.Application/Features/Organization/ReleaseStaff/` | Release staff to Citizen | ✅ Mới |
| `src/Greenlens.Application/Features/Reports/EscalateReport/` | LEO escalate to DEO | ✅ Mới |
| `src/Greenlens.Api/Controllers/InvitationsController.cs` | 3 invitation endpoints | ✅ Mới |
| `src/Greenlens.Api/Controllers/ReportsController.cs` | +escalate endpoint | ✅ Sửa |
| `src/Greenlens.Api/Controllers/LocalOfficesController.cs` | +release staff, updated recruit | ✅ Sửa |

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
- **Non-generic Result uses `ToHttpNoContent()`**, generic `Result<T>` uses `ToHttp()` or `ToHttpCreated()`
- **Domain layer KHÔNG reference Errors class** (Application layer) — dùng inline `new Error(...)` (xem InspectionReport, StaffInvitation)
- **Invitation flow replaces instant recruit** — RecruitStaff creates StaffInvitation, citizen must Accept
- **Reject re-queue:** status stays Submitted, AssignedOfficeId = null → Department queue
- **LEO escalate:** manual POST, not auto-flag — vì 1 phường có cả tuyến cấp TP lẫn hẻm

## 7. Câu hỏi mở / Cần xác nhận
- Module tiếp theo để implement? (Comments? AI retry? Map? Admin?)
- Cần migration mới cho StaffInvitation + PasswordHistory + IsBanned column?
- Unit tests cho invitation flow nên viết ở phiên tiếp theo?

## 8. Thuật ngữ
| Thuật ngữ | Nghĩa |
|---|---|
| LEO | Local Environmental Officer (cán bộ MT xã/phường) |
| DEO | Department Environmental Officer (cán bộ MT sở) |
| CM | Company Manager (quản lý công ty DVMT) |
| CITENCO | Công ty MT Đô thị TP.HCM — đơn vị xử lý tuyến cấp TP |
| BR-ORG-xxx | Business Rule — Organization module |
| BR-AUTH-xxx | Business Rule — Authentication module |
| SLA | Service Level Agreement — thời hạn xử lý report theo severity |

## 9. Change Log
- 2026-06-17 — API Documentation v1.7 + E2E test Company Management (20/20 PASS)
- 2026-06-26 — AGENTS.md "Senior Dev" rules + BR v1.2 sync + OVERVIEW.md update
- 2026-06-26 — Full Gamification module (BR-GAM-001..006): 23 new files, 77/77 tests pass, Hangfire setup
- 2026-06-28 — P0 Blocking (TransactionBehavior + 3 SLA jobs) + Notification module (6 endpoints) + docs
- 2026-06-28 — LEO company dispatch: `GET /v1/companies/my-ward`
- 2026-06-29 — BR-AUTH batch: role assignment, lockout, password history, ban/unban, restore account
- 2026-06-30 — BR-ORG batch: invitation flow, reject re-queue, LEO manual escalate, release staff. Removed IsCityLevelRoute flag (replaced by manual escalation)

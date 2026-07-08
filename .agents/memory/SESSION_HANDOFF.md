# Session Handoff — GreenLens Backend

> **Cập nhật lần cuối:** 2026-07-08 23:23 · **Phiên bản:** 13 · **Agent:** Antigravity (Claude Opus 4.6)

## 0. TL;DR
Backend .NET 9 GreenLens. Phiên 13 fix 2 lỗi startup: (1) DI validation — 4 handler inject `IGenericRepository<>` trực tiếp thay vì specific repo → tạo `IReportDraftRepository` + `IReportSatisfactionRepository`; (2) DB schema mismatch — `consent_accepted_at` + `has_data_consent` thiếu cột → tạo + apply migration. User cập nhật BR-OFF-013 workload limit từ 10→6. Code pushed lên `feat/report-lifecycle-hardening`.

## 1. Mục tiêu & Bối cảnh
- **Mục tiêu tổng thể:** Backend .NET 9 cho ứng dụng báo cáo ô nhiễm môi trường (SU26SE049)
- **Phạm vi phiên 13:** Bug fix startup (DI + migration), stabilization
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
| 16 | BR-ORG-016: **LEO manual escalate** thay vì flag `IsCityLevelRoute` trên Ward | Flag trên Ward không chính xác — 1 phường có cả tuyến cấp TP lẫn hẻm | 2026-06-30 |
| 17 | BR-ORG-021: Invitation flow (7 ngày) + ReleaseStaff | Không instant role change — citizen phải accept | 2026-06-29 |
| 18 | BR-OFF-013 workload limit **6 tasks/team** (cập nhật từ 10) — **deferred** | User cần xem lại logic vận hành trước | 2026-07-08 |
| 19 | BR-OFF-021 KPI: hỗ trợ cả custom date range + preset periods | Linh hoạt cho nhiều use case | 2026-07-07 |
| 20 | BR-OFF-022 Export: cùng 1 endpoint, `format=csv|xlsx` | Thống nhất, dùng ClosedXML cho Excel | 2026-07-07 |
| 21 | BR-DAT-002 Ảnh 2 năm: xóa S3 file, giữ DB record (Url → placeholder) | Giữ metadata cho audit trail | 2026-07-07 |
| 22 | BR-DAT-003 Export: hỗ trợ cả JSON + CSV | Linh hoạt tùy user | 2026-07-07 |
| 23 | BR-DAT-005 Consent: `HasDataConsent` default false, user phải accept khi mở app lần đầu | Tuân thủ GDPR / NĐ-13 | 2026-07-07 |
| 24 | **Specific repo per entity, NEVER inject `IGenericRepository<T>` directly** | Convention trong `IGenericRepository.cs` comment — DI chỉ đăng ký specific interface | 2026-07-08 |
| 25 | **Ignore `PendingModelChangesWarning`** trong EF ConfigureWarnings | User muốn dev environment linh hoạt hơn khi có model changes chưa migrate | 2026-07-08 |

## 3. Trạng thái hiện tại

### ✅ Đã hoàn thành (phiên 13 — 2026-07-08)

**Fix DI startup error (3 inner exceptions):**
- Tạo `IReportDraftRepository` + `ReportDraftRepository` (Application + Infrastructure)
- Tạo `IReportSatisfactionRepository` + `ReportSatisfactionRepository`
- Cập nhật 4 handlers: `SaveDraftCommandHandler`, `GetMyDraftsQueryHandler`, `DeleteDraftCommandHandler`, `RateReportCommandHandler` — đổi `IGenericRepository<>` → specific interface
- Đăng ký DI trong `DependencyInjection.cs`

**Fix DB migration error (`consent_accepted_at` does not exist):**
- Tạo migration `202607072130_AddUserConsentAcceptedAt` (thêm 2 cột: `consent_accepted_at`, `has_data_consent`)
- Apply migration thành công

**User changes:**
- `br_v12_comparison_report.md`: sửa BR-ORG section "11/12 → 11/11", BR-OFF-013 limit "10 → 6"
- `DependencyInjection.cs`: thêm ignore `PendingModelChangesWarning`
- Pushed to `feat/report-lifecycle-hardening` (commit `987a2ee`)

### ✅ Đã hoàn thành (phiên trước — tóm tắt)
- Phiên 12: BR-OFF (11/12: SLA breach, priority, KPI, export) + BR-DAT (5/5: retention, export, consent)
- Phiên 11: System Documentation (Architecture, ERD, Activity Diagrams)
- Phiên 10: BR-AUTH (lockout, password history, ban, restore) + BR-ORG (invitation, reject, escalate)
- Phiên 9: Gamification (BR-GAM-001..006), P0 Blocking, Notifications, Company Dispatch
- Phiên 8 trở về trước: API Docs v1.7, E2E tests, Hangfire, DomainEvent infra

## 4. Việc tiếp theo (Next Steps)
- [ ] Comments module (BR-CMT-001..004)
- [ ] AI Service: BR-AI-006 fallback retry job (AiRetryJob)
- [ ] Map module: BR-MAP-001..012 (heatmap, hotspot, nearby — PostGIS queries)
- [ ] Administration: BR-ADM-004 (notification templates), BR-ADM-005 (gamification config), BR-ADM-006..008, BR-ADM-010 (audit log full), BR-ADM-012 (giám sát công ty)
- [ ] BR-OFF-013: workload limit 6 tasks/team (khi user xác nhận logic)
- [ ] CompanyContractExpiryJob (BR-CMP-007): bidding expired
- [ ] BR-CMP-004: thêm `Terminated` vào CompanyStatus enum
- [ ] BR-CMP-006: Gia hạn / tái ký hợp đồng
- [ ] BR-CMP-013: Suspend/Terminate → push tasks về LEO
- [ ] BR-CMP-020: KPI công ty
- [ ] Cập nhật API Documentation lên v1.8+
- [ ] Unit tests cho invitation flow, escalate, reject re-queue (pending từ phiên 10)

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|---|---|---|
| `docs/BusinessRule/br_v12_comparison_report.md` | So sánh BR v1.2 vs hệ thống | ✅ Updated (phiên 13: sửa counts) |
| `src/Greenlens.Application/Common/Interfaces/Persistence/IReportDraftRepository.cs` | Specific repo interface | ✅ Mới (phiên 13) |
| `src/Greenlens.Application/Common/Interfaces/Persistence/IReportSatisfactionRepository.cs` | Specific repo interface | ✅ Mới (phiên 13) |
| `src/Greenlens.Infrastructure/Persistence/Repositories/ReportDraftRepository.cs` | Repo implementation | ✅ Mới (phiên 13) |
| `src/Greenlens.Infrastructure/Persistence/Repositories/ReportSatisfactionRepository.cs` | Repo implementation | ✅ Mới (phiên 13) |
| `src/Greenlens.Infrastructure/DependencyInjection.cs` | DI container setup | ✅ Sửa (phiên 13: +2 repo, +PendingModelChangesWarning) |
| `src/Greenlens.Infrastructure/Persistence/Migrations/20260707143234_202607072130_AddUserConsentAcceptedAt.cs` | Migration consent columns | ✅ Mới (phiên 13) |
| `src/Greenlens.Application/Features/Reports/RateReport/RateReportCommandHandler.cs` | Rate report handler | ✅ Sửa (specific repo) |
| `src/Greenlens.Application/Features/Reports/SaveDraft/SaveDraftCommandHandler.cs` | Save draft handler | ✅ Sửa (specific repo) |
| `src/Greenlens.Application/Features/Reports/GetMyDrafts/GetMyDraftsQueryHandler.cs` | Get drafts handler | ✅ Sửa (specific repo) |
| `src/Greenlens.Application/Features/Reports/DeleteDraft/DeleteDraftCommandHandler.cs` | Delete draft handler | ✅ Sửa (specific repo) |

## 6. Kiến thức nền & Quy ước
- **Tech stack:** .NET 9, ASP.NET Core, EF Core 9, PostgreSQL + PostGIS, Hangfire, MediatR, FluentValidation, Mapster, ClosedXML
- **Architecture:** Clean Architecture (Domain → Application → Infrastructure ← Api)
- **CQRS:** MediatR Command/Query per vertical slice
- **DomainEvent flow:** Entity.AddDomainEvent → UnitOfWork.SaveChanges → MediatR.Publish after commit
- **Naming:** snake_case DB (UseSnakeCaseNamingConvention), PascalCase C#
- **HasFilter trong EF:** Dùng `"column_name"` (snake_case), KHÔNG `"PropertyName"`
- **Build:** `dotnet build -v q` | **Test:** `dotnet test --no-build`
- **Run:** `dotnet run -lp https` (từ `src/Greenlens.Api/`)
- **Migration tạo:** `dotnet ef migrations add <Name> --project ..\Greenlens.Infrastructure --startup-project .` (từ Api dir)
- **Migration apply:** `dotnet ef database update --project ..\Greenlens.Infrastructure --startup-project .`
- **Git:** Conventional Commits, branch `feature/<slug>`, KHÔNG dùng mã BR/P0 trong tên
- **Non-generic Result uses `ToHttpNoContent()`**, generic `Result<T>` uses `ToHttp()` or `ToHttpCreated()`
- **Domain layer KHÔNG reference Errors class** (Application layer) — dùng inline `new Error(...)`
- **Repo convention:** Mỗi entity phải có `IXxxRepository : IGenericRepository<Xxx>` riêng + impl + DI registration. KHÔNG inject `IGenericRepository<T>` trực tiếp
- **Invitation flow replaces instant recruit** — RecruitStaff creates StaffInvitation, citizen must Accept
- **Reject re-queue:** status stays Submitted, AssignedOfficeId = null → Department queue
- **LEO escalate:** manual POST, not auto-flag — vì 1 phường có cả tuyến cấp TP lẫn hẻm
- **Diagrams:** dùng Mermaid trong .md — xem trên GitHub hoặc mermaid.live
- **Export endpoint pattern:** `?format=csv|xlsx` hoặc `?format=json|csv` — cùng 1 endpoint, content negotiation qua query param
- **ClosedXML** đã thêm vào `Greenlens.Application.csproj` cho Excel export
- **EF Warnings:** ignore `PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning` + `PendingModelChangesWarning`

## 7. Câu hỏi mở / Cần xác nhận
- BR-OFF-013 workload limit **6 tasks/team**: user cần xem lại logic vận hành trước khi implement
- Module tiếp theo: Comments? AI retry? Map? Admin? Company (CMP-004/006/013/020)?
- BR-CMP-004: thêm `Terminated` vào CompanyStatus enum — user cần confirm behavior
- Unit tests cho invitation flow, escalate, reject re-queue — pending từ phiên 10

## 8. Thuật ngữ
| Thuật ngữ | Nghĩa |
|---|---|
| LEO | Local Environmental Officer (cán bộ MT xã/phường) |
| DEO | Department Environmental Officer (cán bộ MT sở) |
| CM | Company Manager (quản lý công ty DVMT) |
| CITENCO | Công ty MT Đô thị TP.HCM — đơn vị xử lý tuyến cấp TP |
| SLA | Service Level Agreement — thời hạn xử lý report theo severity |
| KPI | Key Performance Indicator — chỉ số hiệu suất officer |
| GDPR | General Data Protection Regulation — quy định bảo vệ dữ liệu EU |
| NĐ-13 | Nghị định 13/2023/NĐ-CP — Bảo vệ dữ liệu cá nhân Việt Nam |

## 9. Change Log
- 2026-06-17 — API Documentation v1.7 + E2E test Company Management (20/20 PASS)
- 2026-06-26 — AGENTS.md "Senior Dev" rules + BR v1.2 sync + OVERVIEW.md update
- 2026-06-26 — Full Gamification module (BR-GAM-001..006): 23 new files, 77/77 tests pass, Hangfire setup
- 2026-06-28 — P0 Blocking (TransactionBehavior + 3 SLA jobs) + Notification module (6 endpoints) + docs
- 2026-06-28 — LEO company dispatch: `GET /v1/companies/my-ward`
- 2026-06-29 — BR-AUTH batch: role assignment, lockout, password history, ban/unban, restore account
- 2026-06-30 — BR-ORG batch: invitation flow, reject re-queue, LEO manual escalate, release staff
- 2026-07-01 — System Documentation: Architecture Diagram (8 Mermaid), Conceptual ERD (33 entities), Activity Diagrams (6 flows)
- 2026-07-07 — BR-OFF (11/12): SLA breach notifications, priority score job, KPI query, report export (CSV+XLSX). ClosedXML added. Fix ReportTests.Reject
- 2026-07-07 — BR-DAT (5/5): DataRetentionJob, ExportMyData (JSON+CSV), User consent (HasDataConsent + POST endpoint + SubmitReport guard), migration
- 2026-07-08 — Fix DI startup: tạo IReportDraftRepository + IReportSatisfactionRepository (4 files mới, 4 handlers sửa, DI registration). Fix migration: AddUserConsentAcceptedAt (2 cột). BR-OFF-013 limit 10→6. Pushed feat/report-lifecycle-hardening (987a2ee)

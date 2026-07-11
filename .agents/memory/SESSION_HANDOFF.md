# Session Handoff — GreenLens Backend

> **Cập nhật lần cuối:** 2026-07-10 16:40 · **Phiên bản:** 16 · **Agent:** Antigravity (Gemini 3.5 Flash)

## 0. TL;DR
Backend .NET 9 GreenLens. Phiên 16 hoàn thành **Admin module 12/12 rules** (BR-ADM-001..012): PenaltyFramework CRUD, AuditLog pipeline, Content Moderation (hide/unhide), Spam Dashboard, GamificationConfig CRUD, NotificationTemplate CRUD+publish+test-send, DEO province scoping. Fix regex partial method error, fix 18 CS8602 build warnings, thêm SwaggerOperation cho admin endpoints. Tạo `docs/api-admin-module.md` (15 endpoints). Cập nhật `br_v12_comparison_report.md` (Admin 12/12). **Tổng tiến độ ~70%.**

## 1. Mục tiêu & Bối cảnh
- **Mục tiêu tổng thể:** Backend .NET 9 cho ứng dụng báo cáo ô nhiễm môi trường (SU26SE049)
- **Phạm vi phiên 16:** Admin module (BR-ADM-001..012) implement + API docs + build fixes
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
| 18 | BR-OFF-013 workload limit **6 tasks/team**, cảnh báo tại 5 | User xác nhận | 2026-07-08 |
| 19 | BR-OFF-021 KPI: hỗ trợ cả custom date range + preset periods | Linh hoạt cho nhiều use case | 2026-07-07 |
| 20 | BR-OFF-022 Export: cùng 1 endpoint, `format=csv|xlsx` | Thống nhất, dùng ClosedXML cho Excel | 2026-07-07 |
| 21 | BR-DAT-002 Ảnh 2 năm: xóa S3 file, giữ DB record (Url → placeholder) | Giữ metadata cho audit trail | 2026-07-07 |
| 22 | BR-DAT-003 Export: hỗ trợ cả JSON + CSV | Linh hoạt tùy user | 2026-07-07 |
| 23 | BR-DAT-005 Consent: `HasDataConsent` default false, user phải accept khi mở app lần đầu | Tuân thủ GDPR / NĐ-13 | 2026-07-07 |
| 24 | **Specific repo per entity, NEVER inject `IGenericRepository<T>` directly** | Convention trong `IGenericRepository.cs` comment — DI chỉ đăng ký specific interface | 2026-07-08 |
| 25 | **Ignore `PendingModelChangesWarning`** trong EF ConfigureWarnings | User muốn dev environment linh hoạt hơn | 2026-07-08 |
| 26 | **BR-CMP-004: Suspend/Terminate → phương án A** (auto-decline assignments + revert reports về Verified) | User chọn | 2026-07-08 |
| 27 | **BR-CMP-006: ContractPeriod entity (phương án B)** cho lịch sử kỳ hợp đồng | User chọn | 2026-07-08 |
| 28 | **BR-CMP-006: Chỉ Terminate + Expire làm trước**, gia hạn/tái ký riêng | User yêu cầu | 2026-07-08 |
| 29 | **BR-REP-030: Duplicate detection Tier 1** = geo ≤ 50m + cùng category + ≤ 24h (SQL query) | Đủ tin cậy, free, instant | 2026-07-10 |
| 30 | **BR-REP-030: Tier 2 dùng CLIP/DINOv2**, KHÔNG dùng pHash | pHash fail khi khác góc > 30°. CLIP/DINOv2 hiểu ngữ nghĩa ảnh, xử lý tốt < 90° | 2026-07-10 |
| 31 | **Duplicate check inline (Option A)** trong SubmitHandler | Citizen cần biết ngay "báo cáo có thể trùng" | 2026-07-10 |
| 32 | **Tier 2 chạy trên Python AI service** (thêm endpoint `/api/v1/compare-images`), .NET chỉ gọi HTTP | Cùng pattern với `/classify-moderation-upload`. Team AI tự quản model. Không load ML model vào .NET process | 2026-07-10 |
| 33 | **DINOv2-base recommend** cho production (~330MB RAM, ~200ms/cặp, free, CPU đủ) | Cân bằng accuracy/speed. DINOv2-small (~85MB) nếu cần tiết kiệm | 2026-07-10 |
| 34 | **Admin AuditLog: MediatR pipeline behavior `AuditLogBehavior`** auto-ghi log cho commands implement `IAuditable` | Không ghi thủ công từng handler — cross-cutting concern | 2026-07-10 |
| 35 | **PenaltyFramework: unique index `(CategoryId, ViolationLevel)` chỉ cho active** | Cho phép deactivate rồi tạo mới cùng cặp — HasFilter("is_active = true") | 2026-07-10 |
| 36 | **GamificationConfig: event handler đọc config trực tiếp từ DB** (không cache) | Tránh stale cache, config ít thay đổi | 2026-07-10 |
| 37 | **NotificationTemplate: regex `GeneratedRegex` partial method dùng `[GeneratedRegex]`** | .NET 9 source gen — fix partial method error bằng đổi sang non-partial helper | 2026-07-10 |

## 3. Trạng thái hiện tại

### ✅ Đã hoàn thành (phiên 16 — 2026-07-10)

**Admin Module (BR-ADM-001..012) — 12/12 rules:**
- BR-ADM-001: Admin quản lý user (CreateAccount/UpdateUser/DeleteUser) + audit log
- BR-ADM-002: 8 roles hệ thống, UpdateUserRoleCommand + audit log
- BR-ADM-003: CRUD Category (CreateCategory/UpdateCategory/ArchiveCategory)
- BR-ADM-004: NotificationTemplate entity + CRUD + publish flow + test-send API
- BR-ADM-005: GamificationConfig entity + CRUD, event handler đọc config từ DB
- BR-ADM-006: Content moderation: HideReport/UnhideReport + public queries filter
- BR-ADM-007: Spam dashboard: GetSpamSuspectsQuery heuristic (submit/h, reject/week, AI flag)
- BR-ADM-008: PenaltyFramework entity + CRUD + unique index (CategoryId, ViolationLevel) active
- BR-ADM-009: Phân quyền dữ liệu theo scope (DEO tỉnh, LEO xã, CM company)
- BR-ADM-010: AuditLogBehavior (MediatR pipeline) + AuditLogRetentionJob (12 tháng)
- BR-ADM-011: Backup (infra/DevOps concern)
- BR-ADM-012: DEO chỉ xem company có ServiceArea thuộc tỉnh mình (GetCompaniesQueryHandler)

**Build fixes:**
- Fix `PlaceholderRegex()` partial method error → đổi sang non-partial helper method
- Fix 18 CS8602 warnings trong unit tests (null-forgiving operator `!`)
- Thêm `[SwaggerOperation]` cho toàn bộ admin endpoints

**Documentation:**
- `docs/api-admin-module.md` — 15 endpoints, 6 nhóm, request/response schemas, cURL
- `br_v12_comparison_report.md` — cập nhật Admin 12/12

### ✅ Đã hoàn thành (tóm tắt các phiên trước)
- Phiên 15: Plan BR-REP-030..033 duplicate detection (pending approval)
- Phiên 14: Company module 14/14 ✅ (ContractPeriod, KPI, audit)
- Phiên 13: Fix DI startup, migration, BR-OFF-013 limit 10→6
- Phiên 12: BR-OFF (11/12), BR-DAT (5/5)
- Phiên 11: System Documentation (Architecture, ERD, Activity Diagrams)
- Phiên 10: BR-AUTH batch + BR-ORG batch (invitation, reject, escalate)
- Phiên 9: Gamification, P0 Blocking, Notifications, Company Dispatch
- Phiên 8 trở về trước: API Docs v1.7, E2E tests, Hangfire, DomainEvent infra

## 4. Việc tiếp theo (Next Steps)
- [ ] **BR-REP-030..033: Duplicate detection** — approve plan → implement (~15 files)
- [ ] **Comments module** (BR-CMT-001..004) — entity + CRUD + moderation
- [ ] **Cleanup module gaps** (BR-CLN-002..006): check-in ≤ 200m, update ≥ 1/ngày, ≥ 2 ảnh after
- [ ] **Inspection gaps** (BR-INS-003/004/022/030..032): decline 2h, check-in, repeat offender, SLA, KPI
- [ ] **Map module** (BR-MAP-001..012): heatmap, hotspot, nearby, clustering, Redis cache
- [ ] **AI Service**: BR-AI-006 fallback retry job (AiRetryJob)
- [ ] **BR-AUTH-014**: Brute-force lock (sliding window + Turnstile)
- [ ] **BR-SYS-004/BR-REP-010**: Rate limiting (Redis + ASP.NET middleware)
- [ ] **BR-REP-004**: Word filter (tục tĩu)
- [ ] **BR-REP-011**: EXIF metadata validation
- [ ] Cập nhật API Documentation lên v1.8+
- [ ] Unit tests cho invitation flow, escalate, reject re-queue (pending từ phiên 10)

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|---|---|---|
| `docs/BusinessRule/br_v12_comparison_report.md` | So sánh BR v1.2 vs hệ thống | ✅ Updated (phiên 16: Admin 12/12) |
| `docs/api-admin-module.md` | API docs cho 15 admin endpoints | ✅ Mới (phiên 16) |
| `docs/ai-compare-images-spec.md` | Spec endpoint `/compare-images` cho Python AI service | ✅ Mới (phiên 15) |
| `src/Greenlens.Domain/Entities/PenaltyFramework.cs` | Entity khung phạt | ✅ Mới (phiên 16) |
| `src/Greenlens.Domain/Entities/AuditLog.cs` | Entity audit log | ✅ Mới (phiên 16) |
| `src/Greenlens.Domain/Entities/NotificationTemplate.cs` | Entity template thông báo | ✅ Mới (phiên 16) |
| `src/Greenlens.Domain/Entities/GamificationConfig.cs` | Entity cấu hình điểm | ✅ Mới (phiên 16) |
| `src/Greenlens.Application/Common/Behaviors/AuditLogBehavior.cs` | Pipeline behavior auto audit | ✅ Mới (phiên 16) |
| `src/Greenlens.Application/Common/Interfaces/IAuditable.cs` | Marker interface cho auditable commands | ✅ Mới (phiên 16) |
| `src/Greenlens.Api/Controllers/AdminController.cs` | Admin API endpoints | ✅ Sửa (phiên 16: thêm 15 endpoints) |
| `src/Greenlens.Domain/Entities/Report.cs` | Entity báo cáo — có ParentReportId, MarkDuplicate, ReportFlag, IsHidden | ✅ Sửa (phiên 16: +IsHidden) |
| `src/Greenlens.Domain/Entities/ContractPeriod.cs` | Entity kỳ hợp đồng | ✅ Mới (phiên 14) |
| `src/Greenlens.Infrastructure/AI/AiClassificationService.cs` | HTTP adapter cho Python AI | Cần thêm CompareAsync method |

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
- **Repo convention:** Mỗi entity phải có `IXxxRepository : IGenericRepository<Xxx>` riêng + impl + DI registration
- **Invitation flow replaces instant recruit** — RecruitStaff creates StaffInvitation, citizen must Accept
- **Reject re-queue:** status stays Submitted, AssignedOfficeId = null → Department queue
- **LEO escalate:** manual POST, not auto-flag
- **Export endpoint pattern:** `?format=csv|xlsx` — cùng 1 endpoint
- **EF Warnings:** ignore `PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning` + `PendingModelChangesWarning`
- **ContractPeriod pattern:** 1 company → N periods. CreateCompany tạo initial period
- **CM data isolation:** Mọi CM handler resolve companyId từ token, KHÔNG nhận từ client
- **Duplicate detection:** Tier 1 (geo+time+category, SQL, inline SubmitHandler) + Tier 2 (CLIP/DINOv2 via Python `/api/v1/compare-images`, optional, 5s timeout fallback)
- **Report.Location:** dùng `decimal Latitude/Longitude`, KHÔNG phải `NetTopologySuite.Point`. Geo query dùng Haversine approximate rồi post-filter
- **Admin AuditLog:** IAuditable marker → AuditLogBehavior pipeline → AuditLogger persists. Không ghi thủ công
- **PenaltyFramework unique index:** `(CategoryId, ViolationLevel)` HasFilter `"is_active = true"` — cho phép deactivate rồi tạo mới
- **GeneratedRegex trong .NET 9:** Nếu partial method gặp lỗi accessibility, đổi sang private static helper method thay vì dùng `[GeneratedRegex]` attribute trên partial method

## 7. Câu hỏi mở / Cần xác nhận
- **BR-REP-030..033 plan chưa approved** — user cần review plan rồi confirm
- Module tiếp theo sau duplicate detection: Comments? Map? Cleanup? Inspection?
- Unit tests cho invitation flow, escalate, reject re-queue — pending từ phiên 10
- Python AI service deploy: user cần xác nhận infra (Railway/Render/AWS) cho DINOv2-base (~1.5GB RAM min)
- Admin module branch `feature/admin-module` — user cần commit theo 4-phase commit guide

## 8. Thuật ngữ
| Thuật ngữ | Nghĩa |
|---|---|
| LEO | Local Environmental Officer (cán bộ MT xã/phường) |
| DEO | Department Environmental Officer (cán bộ MT sở) |
| CM | Company Manager (quản lý công ty DVMT) |
| CITENCO | Công ty MT Đô thị TP.HCM — đơn vị xử lý tuyến cấp TP |
| SLA | Service Level Agreement — thời hạn xử lý report theo severity |
| KPI | Key Performance Indicator — chỉ số hiệu suất officer/company |
| pHash | Perceptual Hash — so sánh pixel layout, fail khi khác góc > 30° |
| CLIP | Contrastive Language-Image Pretraining (OpenAI) — image embedding model |
| DINOv2 | Self-supervised Vision Transformer (Meta) — image embedding model, recommend cho GreenLens |
| Tier 1 | Duplicate detection bằng geo+time+category (SQL query) |
| Tier 2 | Duplicate detection bằng AI image compare (CLIP/DINOv2) |

## 9. Change Log
- 2026-06-17 — API Documentation v1.7 + E2E test Company Management (20/20 PASS)
- 2026-06-26 — AGENTS.md "Senior Dev" rules + BR v1.2 sync + OVERVIEW.md update
- 2026-06-26 — Full Gamification module (BR-GAM-001..006): 23 new files, 77/77 tests pass, Hangfire setup
- 2026-06-28 — P0 Blocking (TransactionBehavior + 3 SLA jobs) + Notification module (6 endpoints) + docs
- 2026-06-28 — LEO company dispatch: `GET /v1/companies/my-ward`
- 2026-06-29 — BR-AUTH batch: role assignment, lockout, password history, ban/unban, restore account
- 2026-06-30 — BR-ORG batch: invitation flow, reject re-queue, LEO manual escalate, release staff
- 2026-07-01 — System Documentation: Architecture Diagram (8 Mermaid), Conceptual ERD (33 entities), Activity Diagrams (6 flows)
- 2026-07-07 — BR-OFF (11/12): SLA breach notifications, priority score job, KPI query, report export (CSV+XLSX). ClosedXML added
- 2026-07-07 — BR-DAT (5/5): DataRetentionJob, ExportMyData (JSON+CSV), User consent flow + migration
- 2026-07-08 — Fix DI startup: IReportDraftRepository + IReportSatisfactionRepository. Fix migration. BR-OFF-013 limit 10→6
- 2026-07-10 — Company module 14/14 ✅ (CMP-006 ContractPeriod + RenewContract, CMP-020 KPI, CMP-021 audit). Migration AddContractPeriods. Build 0 errors
- 2026-07-10 — Plan BR-REP-030..033 duplicate detection. Locked: Tier 1 inline + Tier 2 CLIP/DINOv2 via Python. Created ai-compare-images-spec.md. Plan pending approval
- 2026-07-10 — Admin module 12/12 ✅ (BR-ADM-001..012). PenaltyFramework CRUD, AuditLogBehavior pipeline, ContentModeration hide/unhide, SpamDashboard, NotificationTemplate CRUD+publish+test, GamificationConfig CRUD. Fix regex partial method + 18 CS8602 warnings. SwaggerOperation added. docs/api-admin-module.md created. Build 0 warnings

# Handoff Log — GreenLens Backend

- 2026-06-07 — Refactor company dispatch: tách CM quản lý team, CompanyStaff role, Swagger tags Company Dashboard, dual-path team architecture
- 2026-06-08 — Nullable LocalOfficeId + CompanyServiceArea join entity + dispatch validate service area + migration + §1.1 Domain Knowledge VN waste collection vào OVERVIEW.md
- 2026-06-11 — MustChangePassword flow, CreateCompany reworked (kèm CM account + MK tạm), auto-activate company khi CM đổi MK, CreateCompanyStaff + GetCompanyStaff endpoints, migration AddMustChangePasswordToUser
- 2026-06-14 — Loại bỏ NotificationEmail/SendAccountCredentials feature (không gửi email TK/MK), cleanup code + validators + handlers
- 2026-06-14 — Company Management Phase 2: Remove MailKit, CM team CRUD (update/delete), add/remove company team member, toggle staff IsActive, GET /v1/companies/my, 3 error codes mới — 12 files mới + 4 files sửa, build 0 errors
- 2026-06-28 08:41 — Session v8: Tổng hợp tiến độ Gamification module (BR-GAM-001..006) hoàn thành, DomainEvent infra, Hangfire setup. 23 files mới, 77/77 tests pass.
- 2026-06-29 14:30 — Session v9: P0 Blocking (TransactionBehavior + 3 SLA jobs) + Notification module (6 endpoints) + LEO company dispatch API (GET /v1/companies/my-ward). All merged to develop.
- 2026-06-30 16:47 — Session v10: BR-AUTH batch (009/011/012/015/020/021) + BR-ORG batch (014/015/016/021). Invitation flow replaces instant recruit, reject re-queue, LEO manual escalate to DEO, release staff. Removed IsCityLevelRoute flag. Build ✅ 0 errors. Pending commit.
- 2026-07-01 10:08 — Session v11: System Documentation — Architecture Diagram (8 Mermaid), Conceptual ERD (33 entities), Activity Diagrams (6 flows with swimlanes). No code changes.
- 2026-07-07 03:00 — Session v12: BR-OFF (11/12 rules): SLA breach notifications, priority score job, KPI query (custom+preset), report export CSV+XLSX (ClosedXML). BR-DAT (5/5 rules): DataRetentionJob, ExportMyData JSON+CSV, User consent flow + SubmitReport guard + migration. Build 0 errors, 150 tests pass.
- 2026-07-08 23:23 — Session v13: Fix DI startup (IReportDraftRepository + IReportSatisfactionRepository — 4 files mới, 4 handlers sửa). Fix migration AddUserConsentAcceptedAt (2 cột). BR-OFF-013 limit 10→6. Ignore PendingModelChangesWarning. Pushed feat/report-lifecycle-hardening (987a2ee).

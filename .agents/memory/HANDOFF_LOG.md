# Handoff Log — GreenLens Backend

- 2026-06-07 — Refactor company dispatch: tách CM quản lý team, CompanyStaff role, Swagger tags Company Dashboard, dual-path team architecture
- 2026-06-08 — Nullable LocalOfficeId + CompanyServiceArea join entity + dispatch validate service area + migration + §1.1 Domain Knowledge VN waste collection vào OVERVIEW.md
- 2026-06-11 — MustChangePassword flow, CreateCompany reworked (kèm CM account + MK tạm), auto-activate company khi CM đổi MK, CreateCompanyStaff + GetCompanyStaff endpoints, migration AddMustChangePasswordToUser
- 2026-06-14 — Loại bỏ NotificationEmail/SendAccountCredentials feature (không gửi email TK/MK), cleanup code + validators + handlers
- 2026-06-14 — Company Management Phase 2: Remove MailKit, CM team CRUD (update/delete), add/remove company team member, toggle staff IsActive, GET /v1/companies/my, 3 error codes mới — 12 files mới + 4 files sửa, build 0 errors
- 2026-06-28 08:41 — Session v8: Tổng hợp tiến độ Gamification module (BR-GAM-001..006) hoàn thành, DomainEvent infra, Hangfire setup. 23 files mới, 77/77 tests pass.

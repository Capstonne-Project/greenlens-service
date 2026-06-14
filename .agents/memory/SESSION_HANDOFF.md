# Session Handoff — GreenLens v1.8: Automated Onboarding & CompanyStaff Management

> Cập nhật lần cuối: 2026-06-14 18:10 · Phiên bản: 3 · Agent: Antigravity

## 0. TL;DR (đọc trước tiên)

Đã hoàn thành luồng **Automated Onboarding** (DEO tạo Công ty + CM account với MK tạm → CM đăng nhập đổi MK → công ty auto-activate) và **CM Staff Management** (CM tạo CompanyStaff account với MK tạm, optional gán team). Tính năng gửi TK/MK qua email cá nhân đã được **loại bỏ** — DEO/CM copy MK tạm từ response gửi thủ công. Migration `AddMustChangePasswordToUser` đã tạo. **Việc tiếp theo**: CRUD team công ty mở rộng (update/delete), add/remove member khỏi team, và testing end-to-end.

## 1. Mục tiêu & Bối cảnh

- **Mục tiêu tổng thể:** Xây dựng hệ thống quản lý tổ chức hoàn chỉnh cho GreenLens — từ DEO quản lý công ty DVMT đến CM quản lý nhân sự, kết nối với luồng dispatch report.
- **Phạm vi:** Domain entities, Application CQRS handlers, EF config, migrations, API controllers, docs.
- **Ngôn ngữ làm việc:** Tiếng Việt (giao tiếp + Swagger docs), C# 13 / .NET 9 (code).

## 2. Quyết định đã chốt (Locked Decisions)

| # | Quyết định | Lý do | Ngày |
|---|---|---|---|
| 1 | `EnvironmentalTeam.LocalOfficeId` → `Guid?` (nullable cho company team) | Company team không gắn cố định 1 phường mà đi theo task | 2026-06-07 |
| 2 | `CompanyServiceArea` (CompanyId + WardCode) validate dispatch | Biết company nào phụ trách ward nào, 1-N:N-1 | 2026-06-07 |
| 3 | Company team chỉ loại `Cleanup`. InspectionTeam thuộc phường (LEO managed) | Xử phạt = chức năng hành chính, không thuộc doanh nghiệp | 2026-06-07 |
| 4 | `CompanyStaff` role tách biệt với `Cleaner` (cùng workflow, khác org) | Phân biệt nhân viên công ty vs tình nguyện viên cộng đồng | 2026-06-07 |
| 5 | LEO giữ mô hình **Recruit** (tìm Citizen có sẵn). CM dùng **Create** (tạo account mới) | LEO tuyển từ dân, CM tạo account cho nhân viên doanh nghiệp | 2026-06-11 |
| 6 | `User.MustChangePassword` flag — cơ chế ép đổi MK lần đầu | Áp dụng chung cho tất cả account tạo bởi admin (DEO→CM, CM→Staff) | 2026-06-11 |
| 7 | `User.CreateWithTempPassword()` — factory method chung | Tái sử dụng cho cả tạo CM và tạo CompanyStaff | 2026-06-11 |
| 8 | Khi CM đổi MK lần đầu → công ty auto-activate (`PendingActivation` → `Active`) | Thay thế API ActivateCompany cũ, UX tốt hơn | 2026-06-11 |
| 9 | **KHÔNG gửi email TK/MK** — DEO/CM copy từ response gửi thủ công | Email đăng nhập ≠ email cá nhân; phức tạp hóa quá mức cho capstone | 2026-06-14 |
| 10 | MailKit đã install nhưng không dùng cho credentials; hệ thống email chỉ dùng OTP/reset password | Giữ `IEmailSender` cho OTP + password reset; bỏ `SendAccountCredentialsAsync` | 2026-06-14 |

## 3. Trạng thái hiện tại

### Đã hoàn thành:
- ✅ **MustChangePassword flow**: flag trên User entity, auto-clear khi đổi MK, trả về trong LoginResponse
- ✅ **CreateCompany reworked**: DEO tạo công ty + CM account + MK tạm trong 1 request
- ✅ **ChangePasswordHandler**: auto-activate company khi CM đổi MK lần đầu
- ✅ **ActivateCompany endpoint**: đã xóa (thay thế bằng auto-activate)
- ✅ **CreateCompanyStaff**: CM tạo CompanyStaff (email + MK tạm + optional team assignment)
- ✅ **GetCompanyStaff**: CM xem danh sách nhân viên (kèm team info, pagination, filter isActive)
- ✅ **CompaniesController endpoints**: `POST /v1/companies/my/staff`, `GET /v1/companies/my/staff`
- ✅ **Migration**: `AddMustChangePasswordToUser` (boolean, default false) — đã tạo, chưa apply
- ✅ **NotificationEmail feature**: đã implement rồi **đã loại bỏ hoàn toàn** (quyết định #9)
- ✅ **Build**: 0 errors trên toàn solution
- ✅ **Docs**: `API_DASHBOARD_AND_FLOW.md` cập nhật đầy đủ 91 endpoints theo role

### Chưa làm:
- ⬜ Apply migration lên database (cần `dotnet ef database update`)
- ⬜ CRUD team mở rộng (update/delete company team)
- ⬜ Add/remove member khỏi team
- ⬜ Testing end-to-end luồng onboarding

## 4. Việc tiếp theo (Next Steps)

- [ ] Apply migration `AddMustChangePasswordToUser` lên database
- [ ] CM CRUD team mở rộng: `PUT /v1/teams/company-teams/{id}`, `DELETE /v1/teams/company-teams/{id}`
- [ ] CM add/remove staff from team: `POST /v1/teams/{id}/members`, `DELETE /v1/teams/{id}/members/{userId}`
- [ ] CM deactivate/reactivate staff: `PUT /v1/companies/my/staff/{userId}/status`
- [ ] Testing end-to-end: DEO tạo company → CM login + đổi MK → company active → CM tạo team + staff → Staff login + đổi MK
- [ ] 3 vấn đề tiềm ẩn từ session trước (ServiceScope, ContractType mở rộng, HTX identity) — user chưa quyết

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|---|---|---|
| `src/Greenlens.Domain/Entities/User.cs` | `MustChangePassword` flag + `CreateWithTempPassword()` factory | đã sửa ✅ |
| `src/Greenlens.Domain/Entities/CompanyStaff.cs` | Join entity User ↔ Company | giữ nguyên |
| `src/Greenlens.Application/Features/Auth/Login/LoginResponse.cs` | `UserDto` + `MustChangePassword` field | đã sửa ✅ |
| `src/Greenlens.Application/Features/Auth/Login/LoginCommandHandler.cs` | Trả MustChangePassword trong response | đã sửa ✅ |
| `src/Greenlens.Application/Features/Auth/ChangePassword/ChangePasswordCommandHandler.cs` | Auto-activate company khi CM đổi MK | đã sửa ✅ |
| `src/Greenlens.Application/Features/Organization/CreateCompany/` | Command + Handler + Validator (reworked: tạo CM account kèm) | đã sửa ✅ |
| `src/Greenlens.Application/Features/Organization/CreateCompanyStaff/` | **MỚI** — Command + Handler + Validator (CM tạo staff) | mới ✅ |
| `src/Greenlens.Application/Features/Organization/GetCompanyStaff/` | **MỚI** — Query + Handler (CM list staff + team info) | mới ✅ |
| `src/Greenlens.Application/Common/Interfaces/IEmailSender.cs` | OTP + password reset only (đã bỏ SendAccountCredentials) | đã sửa ✅ |
| `src/Greenlens.Infrastructure/Email/SmtpEmailSender.cs` | SMTP impl — bỏ credentials method | đã sửa ✅ |
| `src/Greenlens.Api/Controllers/CompaniesController.cs` | + `POST my/staff`, `GET my/staff`, bỏ ActivateCompany | đã sửa ✅ |
| `src/Greenlens.Infrastructure/Persistence/Migrations/20260611142842_AddMustChangePasswordToUser.cs` | **MỚI** — migration chưa apply | mới ⚠️ |
| `docs/API_DASHBOARD_AND_FLOW.md` | API reference by role + report lifecycle diagram (91 endpoints) | đã sửa ✅ |

## 6. Kiến thức nền & Quy ước

- **Tech stack:** .NET 9, ASP.NET Core 9, EF Core 9, PostgreSQL + PostGIS, Clean Architecture
- **Quy ước code:** C# 13, file-scoped namespace, primary constructor, sealed class, `Result<T>` pattern, FluentValidation, MediatR CQRS
- **Lệnh hay dùng:**
  - Build: `dotnet build` (kill `dotnet.exe` trước nếu đang chạy — tránh file-lock MSB3021)
  - Migration add: `dotnet ef migrations add <Name> --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api`
  - Migration apply: `dotnet ef database update --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api`
- **Kiến thức nghiệp vụ:**
  - Onboarding flow: DEO tạo → CM đổi MK → company active → CM tạo staff → staff đổi MK
  - `MustChangePassword` áp dụng cho MỌI account tạo bởi admin
  - LEO = Recruit (tìm Citizen đã có), CM = Create (tạo account mới)
  - CompanyStaff ≠ Cleaner (cùng workflow, khác role/org)
  - Email system chỉ dùng cho OTP + password reset (KHÔNG dùng gửi credentials)

## 7. Câu hỏi mở / Cần xác nhận

- 3 vấn đề tiềm ẩn từ session trước: ServiceScope, ContractType mở rộng, HTX identity
- MailKit package đã install (4.17.0) nhưng không dùng — có muốn remove khỏi csproj?
- CM deactivate staff: cần soft-delete hay chỉ toggle `IsActive`?
- Nên có endpoint cho CM xem profile công ty mình không? (`GET /v1/companies/my`)

## 8. Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| LEO | Local Environmental Officer — cán bộ môi trường cấp xã/phường |
| DEO | Department Environmental Officer — cán bộ cấp tỉnh/thành |
| CM | CompanyManager — quản lý công ty môi trường |
| CS | CompanyStaff — nhân viên hiện trường của công ty |
| MustChangePassword | Cờ ép đổi MK lần đầu, tự clear khi user gọi ChangePassword |
| TempPassword | MK tạm sinh random 10 ký tự, chỉ hiển thị 1 lần trong response |
| ServiceArea | Bảng mapping company → ward (CompanyServiceArea) |

## 9. Change Log

- 2026-06-07 — Refactor company dispatch: tách CM quản lý team, CompanyStaff role, Swagger tags
- 2026-06-08 — Nullable LocalOfficeId + CompanyServiceArea + dispatch validation + migration + §1.1 Domain Knowledge
- 2026-06-11 — MustChangePassword flow + CreateCompany reworked (CM account kèm) + auto-activate + CreateCompanyStaff + GetCompanyStaff + migration
- 2026-06-14 — Loại bỏ NotificationEmail/SendAccountCredentials (quyết định không gửi email TK/MK) + cleanup code

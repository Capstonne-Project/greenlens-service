# Session Handoff — GreenLens v1.3 Company Dispatch & Service Area

> Cập nhật lần cuối: 2026-06-09 20:02 · Phiên bản: 1 · Agent: Antigravity

## 0. TL;DR (đọc trước tiên)

Đang refactor kiến trúc company dispatch: company team **không gắn cố định LocalOffice** nữa (nullable), vùng phục vụ quản lý qua **CompanyServiceArea** (CompanyId + WardCode). Dispatch validate company phải phụ trách ward của report. Đã hoàn thành code + migration + OVERVIEW.md. **Việc tiếp theo**: giải quyết 3 vấn đề tiềm ẩn (ServiceScope, ContractType mở rộng, HTX identity) — user đang suy nghĩ.

## 1. Mục tiêu & Bối cảnh

- **Mục tiêu tổng thể:** Decouple company-affiliated teams từ ràng buộc cứng LocalOffice (cấp phường/xã), cho phép 1 công ty phụ trách nhiều phường/xã linh hoạt (VD: DVCI Quận 3 → P. Bàn Cờ, P. Xuân Hòa, P. Nhiêu Lộc).
- **Phạm vi:** Domain entity, Application CQRS handlers, EF config, migration, OVERVIEW.md documentation.
- **Bối cảnh thực tế VN:** Hệ thống 3 tầng thu gom rác (CITENCO tầng TW → DVCI tầng Quận → HTX/Tổ tự quản tầng dân lập). 1 phường có thể có nhiều đơn vị thu gom cùng lúc. Nông thôn thường chỉ có community team (không có company).

## 2. Quyết định đã chốt (Locked Decisions)

| # | Quyết định | Lý do | Ngày |
|---|---|---|---|
| 1 | `EnvironmentalTeam.LocalOfficeId` → `Guid?` (nullable). Community team = required, company team = null | Company team không gắn cố định 1 phường/xã mà đi theo task. VD: DVCI Q3 có team phục vụ 3 phường khác nhau | 2026-06-07 |
| 2 | Thêm entity `CompanyServiceArea` (CompanyId + WardCode) | Cần biết company nào phụ trách ward nào để validate dispatch. 1 company → nhiều wards, 1 ward → nhiều companies | 2026-06-07 |
| 3 | Dispatch validate service area: `COMPANY_DOES_NOT_SERVE_WARD` | Ngăn LEO dispatch report đến company không phụ trách ward đó | 2026-06-07 |
| 4 | `CreateCompanyTeam()` bỏ param `localOfficeId` | Company team tạo ra không gắn office, CM tự quản lý | 2026-06-07 |
| 5 | Company team chỉ được tạo loại `Cleanup`. InspectionTeam luôn thuộc phường/xã (LEO managed) | BR-CMP-004, BR-INS-001. Đội xử phạt là chức năng hành chính, không thuộc doanh nghiệp | 2026-06-07 |
| 6 | Mọi CRUD team công ty do CompanyManager thực hiện, LEO chỉ dispatch task đến company | Phân tách trách nhiệm: LEO quản lý lãnh thổ, CM quản lý nguồn lực công ty | 2026-06-07 |
| 7 | Swagger tag cho endpoint CM: `🏢 Company Dashboard` (không phải "Company Dispatch") | CM quản lý nhiều thứ (team, nhân sự, queue), không chỉ dispatch | 2026-06-07 |
| 8 | `CompanyStaff` role tách biệt với `Cleaner` dù hoạt động giống nhau | Phân biệt rõ nhân viên công ty vs tình nguyện viên cộng đồng. Cùng workflow nhưng khác role, khác org | 2026-06-07 |
| 9 | `ContractType` hiện tại: `Subsidiary` + `Bidding` — giữ cho MVP | Đủ phân biệt trực thuộc vs đấu thầu. Có thể thêm `DirectOrder` sau (non-breaking) | 2026-06-08 |

## 3. Trạng thái hiện tại

### Đã hoàn thành:
- ✅ `EnvironmentalTeam.LocalOfficeId` → nullable + factory methods updated
- ✅ `CompanyServiceArea` entity + EF config (unique index `CompanyId, WardCode`)
- ✅ `EnvironmentalServiceCompany.ServiceAreas` navigation
- ✅ `CompanyServiceAreaConfiguration.cs` — FK to Company (cascade) + Ward (restrict)
- ✅ `ApplicationDbContext` — thêm `DbSet<CompanyServiceArea>`
- ✅ `CreateCompanyTeamCommand` — bỏ `LocalOfficeId`
- ✅ `CreateCompanyTeamCommandHandler` — chỉ cần `Name`, auto-resolve CompanyId từ token
- ✅ `DispatchToCompanyCommandHandler` — validate `ServesWardAsync(companyId, wardCode)`
- ✅ `IEnvironmentalServiceCompanyRepository.ServesWardAsync()` + implementation
- ✅ `ReportErrors.CompanyDoesNotServeWard` error code
- ✅ Tất cả response DTOs: `LocalOfficeId` → `Guid?` (TeamItem, TeamDetailResponse, CompanyTeamItem, CreateTeamResponse)
- ✅ Migration: `MakeCompanyTeamOfficeNullableAndAddServiceAreas` — applied thành công
- ✅ Build: 0 errors
- ✅ TeamsController Swagger docs cập nhật (bỏ "tại một LocalOffice", thêm note team không gắn office)
- ✅ OVERVIEW.md: thêm **§1.1 Domain Knowledge** — vận hành thu gom rác thải VN (3 tầng, đấu thầu, TP.HCM 168 phường, 22 DVCI, nông thôn)

### 3 vấn đề tiềm ẩn (user đang suy nghĩ, CHƯA giải quyết):
1. **ServiceScope** — CITENCO vs DVCI cùng phường nhưng khác phạm vi (trục chính vs hẻm). Cân nhắc thêm enum `ServiceScope` hoặc text field `Description/Scope` trên company.
2. **ContractType mở rộng** — có thể thêm `DirectOrder` (đặt hàng trực tiếp tại tỉnh lẻ, chỉ 1 công ty). Non-breaking change.
3. **HTX identity** — HTX thu tiền rác, tự vận hành (giống doanh nghiệp nhỏ) nhưng không có hợp đồng nhà nước. Hiện map vào community team. Dài hạn có thể thêm `TeamOrigin` enum.

## 4. Việc tiếp theo (Next Steps)

- [ ] User quyết định 3 vấn đề tiềm ẩn (ServiceScope, ContractType, HTX)
- [ ] API endpoints để DEO quản lý `CompanyServiceArea` (CRUD ward → company mapping)
- [ ] Frontend sync: company-managed team creation (body chỉ còn `Name`)
- [ ] Frontend sync: LEO dispatch form hiển thị companies phụ trách ward của report
- [ ] Seed data: tạo 22 DVCI + CITENCO + ServiceArea mapping cho 168 phường TP.HCM (nếu cần demo)

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|---|---|---|
| `src/Greenlens.Domain/Entities/EnvironmentalTeam.cs` | Team entity — LocalOfficeId nullable, 2 factory (Create, CreateCompanyTeam) | đã sửa ✅ |
| `src/Greenlens.Domain/Entities/CompanyServiceArea.cs` | **MỚI** — join entity CompanyId + WardCode | mới ✅ |
| `src/Greenlens.Domain/Entities/EnvironmentalServiceCompany.cs` | Company entity — thêm navigation ServiceAreas | đã sửa ✅ |
| `src/Greenlens.Domain/Enums/ContractType.cs` | Subsidiary / Bidding (chưa thêm DirectOrder) | giữ nguyên |
| `src/Greenlens.Infrastructure/Persistence/Configurations/Organization/CompanyServiceAreaConfiguration.cs` | **MỚI** — EF config, unique (CompanyId, WardCode) | mới ✅ |
| `src/Greenlens.Infrastructure/Persistence/Configurations/Organization/EnvironmentalTeamConfiguration.cs` | FK LocalOfficeId → IsRequired(false) | đã sửa ✅ |
| `src/Greenlens.Infrastructure/Persistence/Repositories/EnvironmentalServiceCompanyRepository.cs` | Thêm ServesWardAsync() | đã sửa ✅ |
| `src/Greenlens.Application/Features/Organization/CreateCompanyTeam/` | Command + Handler — bỏ LocalOfficeId | đã sửa ✅ |
| `src/Greenlens.Application/Features/Reports/DispatchToCompany/DispatchToCompanyCommandHandler.cs` | Validate service area trước dispatch | đã sửa ✅ |
| `src/Greenlens.Application/Common/Errors/ReportErrors.cs` | Thêm CompanyDoesNotServeWard | đã sửa ✅ |
| `src/Greenlens.Api/Controllers/TeamsController.cs` | Swagger docs cập nhật, bỏ 404 Office | đã sửa ✅ |
| `OVERVIEW.md` | Thêm §1.1 Domain Knowledge (A–F) | đã sửa ✅ |
| `docs/report-workflow-v1.3-direct-dispatch.md` | Dispatch workflow docs | giữ nguyên |

## 6. Kiến thức nền & Quy ước

- **Tech stack:** .NET 9, ASP.NET Core 9, EF Core 9, PostgreSQL + PostGIS, Clean Architecture (Domain → Application → Infrastructure → Api)
- **Quy ước code:** C# 13, file-scoped namespace, primary constructor, sealed class, `Result<T>` pattern (không throw exception từ Application layer), FluentValidation, MediatR CQRS
- **Lệnh hay dùng:**
  - Build: `dotnet build` (phải kill Greenlens.Api.exe trước nếu đang chạy)
  - Migration: `dotnet ef migrations add <Name> --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api -- --environment Development`
  - Apply: `dotnet ef database update --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api -- --environment Development`
- **Kiến thức nghiệp vụ quan trọng:**
  - 3 tầng thu gom: CITENCO (TW) → DVCI (Quận) → HTX/Tổ tự quản (dân lập)
  - 1 phường có thể nhiều company cùng phục vụ (khác scope)
  - Hợp đồng đấu thầu có thời hạn (1/3/5 năm)
  - Nông thôn = community team only, không có company
  - LEO = người hiểu địa bàn, tự quyết dispatch
  - CompanyStaff ≠ Cleaner (cùng workflow, khác role/org)

## 7. Câu hỏi mở / Cần xác nhận

- User đang cân nhắc 3 vấn đề tiềm ẩn — chờ quyết định
- Chưa có API endpoint để DEO quản lý CompanyServiceArea (CRUD) — cần thiết kế
- Seed data cho 168 phường TP.HCM + 22 DVCI — có cần cho MVP/demo không?

## 8. Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| LEO | Local Environmental Officer — cán bộ môi trường cấp xã/phường |
| DEO | Department Environmental Officer — cán bộ cấp tỉnh/thành |
| CM | CompanyManager — quản lý công ty môi trường |
| CS | CompanyStaff — nhân viên hiện trường của công ty |
| CITENCO | Công ty TNHH MTV Môi trường Đô thị TP.HCM (tầng TW) |
| DVCI | Dịch Vụ Công Ích — công ty vệ sinh môi trường cấp quận |
| HTX | Hợp tác xã vệ sinh môi trường (dân lập) |
| ServiceArea | Bảng mapping company → ward (CompanyServiceArea) |
| Community team | EnvironmentalTeam có CompanyId == null (LEO quản lý) |
| Company team | EnvironmentalTeam có CompanyId != null, LocalOfficeId == null (CM quản lý) |

## 9. Change Log

- 2026-06-07 — Session gốc: Refactor company dispatch (tách CM khỏi LEO, Swagger tags, CompanyStaff role, CRUD team endpoints)
- 2026-06-08 — Nullable LocalOfficeId + CompanyServiceArea entity + dispatch validation + migration applied + §1.1 Domain Knowledge vào OVERVIEW.md

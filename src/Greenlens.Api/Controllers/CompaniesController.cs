using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.CreateCompany;
using Greenlens.Application.Features.Organization.CreateCompanyManager;
using Greenlens.Application.Features.Organization.CreateCompanyStaff;
using Greenlens.Application.Features.Organization.ResetCompanyManagerPassword;
using Greenlens.Application.Features.Organization.GetCompanies;
using Greenlens.Application.Features.Organization.GetOfficeCompanies;
using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Application.Features.Organization.GetCompanyServiceAreas;
using Greenlens.Application.Features.Organization.GetCompanyStaff;
using Greenlens.Application.Features.Organization.GetMyCompany;
using Greenlens.Application.Features.Organization.ReactivateCompany;
using Greenlens.Application.Features.Organization.SuspendCompany;
using Greenlens.Application.Features.Organization.TerminateCompany;
using Greenlens.Application.Features.Organization.ToggleCompanyStaffStatus;
using Greenlens.Application.Features.Organization.UpdateCompanyServiceAreas;
using Greenlens.Application.Features.Organization.RenewContract;
using Greenlens.Application.Features.Organization.GetContractHistory;
using Greenlens.Application.Features.Organization.GetCompanyKpi;
using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý Công ty Dịch vụ Môi trường (DEO/Admin).</summary>
[ApiController]
[Route("v1/companies")]
[Authorize]
[Produces("application/json")]
public sealed class CompaniesController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  CRUD (BR-CMP-001)
    // ═══════════════════════════════════════════

    [HttpPost]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Tạo công ty DVMT",
        Description = "Tạo Công ty Dịch vụ Môi trường (trực thuộc/đấu thầu). " +
            "Có thể truyền WardCodes để gán phường/xã phụ trách ngay khi tạo. " +
            "ManagerEmail + ManagerFullName là tuỳ chọn — bỏ trống để tạo công ty trước, tạo CM sau qua POST /{id}/manager. " +
            "Nếu có CM: trạng thái ban đầu PendingActivation; CM đăng nhập bằng MK tạm → đổi MK → công ty tự động Active. " +
            "⚠️ TempPassword chỉ hiển thị 1 lần — DEO cần gửi cho CM.")]
    [SwaggerResponse(201, "Đã tạo công ty", typeof(ApiResponse<CreateCompanyResponse>))]
    [SwaggerResponse(404, "Department hoặc WardCode không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(409, "Số hợp đồng hoặc email CM đã tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateCompanyCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPost("{id:guid}/manager")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Tạo tài khoản CM cho công ty",
        Description = "Tạo tài khoản CompanyManager cho công ty đã tồn tại. " +
            "Dùng khi công ty được tạo trước mà chưa có CM, hoặc muốn thêm CM. " +
            "CM đăng nhập bằng MK tạm → đổi MK → công ty tự động Active. " +
            "⚠️ TempPassword chỉ hiển thị 1 lần — DEO cần gửi cho CM.")]
    [SwaggerResponse(201, "Đã tạo tài khoản CM", typeof(ApiResponse<CreateCompanyManagerResponse>))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(409, "Email CM đã tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> CreateManagerAsync(
        [FromRoute] Guid id, [FromBody] CreateCompanyManagerRequest request, CancellationToken ct)
        => (await sender.Send(new CreateCompanyManagerCommand(id, request.ManagerEmail, request.ManagerFullName), ct))
            .ToHttpCreated();

    [HttpPost("{id:guid}/manager/{userId:guid}/reset-password")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Reset mật khẩu CM",
        Description = "Tạo mật khẩu tạm mới cho CompanyManager khi DEO thất lạc TempPassword ban đầu. " +
            "CM bắt buộc đổi MK khi đăng nhập lần tiếp. ⚠️ TempPassword chỉ hiển thị 1 lần.")]
    [SwaggerResponse(200, "Mật khẩu tạm mới", typeof(ApiResponse<ResetCompanyManagerPasswordResponse>))]
    [SwaggerResponse(404, "Công ty hoặc CM không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> ResetManagerPasswordAsync(
        [FromRoute] Guid id, [FromRoute] Guid userId, CancellationToken ct)
        => (await sender.Send(new ResetCompanyManagerPasswordCommand(id, userId), ct)).ToHttp();

    [HttpGet("my-ward")]
    [Authorize(Roles = "LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Công ty phục vụ phường/xã của tôi",
        Description = "Trả về danh sách công ty đang hoạt động (Active) có vùng phục vụ bao gồm phường/xã mà LEO đang quản lý. Tự động xác định office từ tài khoản LEO đang đăng nhập.")]
    [SwaggerResponse(200, "Danh sách công ty", typeof(ApiResponse<GetOfficeCompaniesResponse>))]
    public async Task<IActionResult> GetMyWardCompaniesAsync(CancellationToken ct)
        => (await sender.Send(new GetOfficeCompaniesQuery(), ct)).ToHttp();

    [HttpGet]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Danh sách công ty DVMT",
        Description = "Trả về danh sách công ty DVMT với phân trang, tìm kiếm (tên, mã HĐ, MST), " +
            "lọc theo trạng thái. Sắp xếp: name, status, contractNumber (mặc định: mới nhất).")]
    [SwaggerResponse(200, "Danh sách công ty", typeof(ApiResponse<GetCompaniesResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] CompanyStatus? status = null, [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompaniesQuery(page, pageSize, status, search, sortBy, sortDesc), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Chi tiết công ty DVMT",
        Description = "Trả về thông tin chi tiết công ty kèm danh sách phường phụ trách và số nhân sự.")]
    [SwaggerResponse(200, "Chi tiết công ty", typeof(ApiResponse<CompanyDetailResponse>))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetCompanyByIdQuery(id), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  COMPANY STATUS (BR-CMP-004)
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/suspend")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Tạm ngưng công ty",
        Description = "Tạm ngưng hoạt động công ty (Active → Suspended). " +
            "Cascading: tự động hủy tất cả task đang giao, đưa báo cáo về Verified. " +
            "DEO có thể kích hoạt lại sau bằng endpoint reactivate.")]
    [SwaggerResponse(200, "Đã tạm ngưng", typeof(ApiResponse))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Công ty không Active hoặc lý do quá ngắn", typeof(ApiResponse))]
    public async Task<IActionResult> SuspendAsync(
        [FromRoute] Guid id, [FromBody] CompanyStatusReasonRequest request, CancellationToken ct)
        => (await sender.Send(new SuspendCompanyCommand(id, request.Reason), ct)).ToHttpNoContent("Đã tạm ngưng công ty.");

    [HttpPost("{id:guid}/terminate")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Chấm dứt hợp đồng công ty",
        Description = "Chấm dứt hợp đồng công ty sớm (Active/Suspended/Expired → Terminated). " +
            "Cascading: tự động hủy tất cả task đang giao, đưa báo cáo về Verified. " +
            "⚠️ Hành động không thể đảo ngược.")]
    [SwaggerResponse(200, "Đã chấm dứt", typeof(ApiResponse))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Không thể chấm dứt từ trạng thái hiện tại", typeof(ApiResponse))]
    public async Task<IActionResult> TerminateAsync(
        [FromRoute] Guid id, [FromBody] CompanyStatusReasonRequest request, CancellationToken ct)
        => (await sender.Send(new TerminateCompanyCommand(id, request.Reason), ct)).ToHttpNoContent("Đã chấm dứt hợp đồng.");

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Kích hoạt lại công ty",
        Description = "Kích hoạt lại công ty đang bị tạm ngưng (Suspended → Active). " +
            "Công ty sẽ trở lại hoạt động và nhận task mới.")]
    [SwaggerResponse(200, "Đã kích hoạt lại", typeof(ApiResponse))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Công ty không ở trạng thái Suspended", typeof(ApiResponse))]
    public async Task<IActionResult> ReactivateAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new ReactivateCompanyCommand(id), ct)).ToHttpNoContent("Đã kích hoạt lại công ty.");

    // ═══════════════════════════════════════════
    // ██  SERVICE AREAS (BR-CMP-008, BR-CMP-014)
    // ═══════════════════════════════════════════

    [HttpGet("{id:guid}/service-areas")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Danh sách phường/xã do công ty phụ trách",
        Description = "Trả về tất cả ward mà công ty đang được giao phụ trách. " +
            "Dùng cho màn hình quản lý địa bàn công ty.")]
    [SwaggerResponse(200, "Danh sách service areas", typeof(ApiResponse<GetCompanyServiceAreasResponse>))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> GetServiceAreasAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetCompanyServiceAreasQuery(id), ct)).ToHttp();

    [HttpPut("{id:guid}/service-areas")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Cập nhật địa bàn phụ trách của công ty",
        Description = "Thay thế toàn bộ danh sách ward mà công ty phụ trách. " +
            "Ward không có trong danh sách mới sẽ bị xóa, ward mới sẽ được thêm. " +
            "Gửi mảng rỗng để xóa tất cả.")]
    [SwaggerResponse(204, "Đã cập nhật")]
    [SwaggerResponse(404, "Công ty hoặc ward không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateServiceAreasAsync(
        [FromRoute] Guid id, [FromBody] UpdateServiceAreasRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateCompanyServiceAreasCommand(id, request.WardCodes), ct))
            .ToHttpNoContent("Đã cập nhật địa bàn phụ trách.");

    // ═══════════════════════════════════════════
    // ██  COMPANY STAFF (CM Dashboard - BR-CMP-004)
    // ═══════════════════════════════════════════

    [HttpPost("my/staff")]
    [Authorize(Roles = "CompanyManager")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CM] Tạo tài khoản nhân viên công ty",
        Description = "CM tạo tài khoản CompanyStaff (email + MK tạm). " +
            "Staff đăng nhập lần đầu → bắt buộc đổi MK. " +
            "Có thể gán luôn vào team (nếu truyền teamId). " +
            "⚠️ TempPassword chỉ hiển thị 1 lần.")]
    [SwaggerResponse(201, "Đã tạo nhân viên", typeof(ApiResponse<CreateCompanyStaffResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    [SwaggerResponse(409, "Email đã tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> CreateStaffAsync(
        [FromBody] CreateCompanyStaffCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpGet("my/staff")]
    [Authorize(Roles = "CompanyManager")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CM] Danh sách nhân viên công ty",
        Description = "Trả về danh sách nhân viên thuộc công ty của CM, " +
            "kèm thông tin team đang tham gia. Hỗ trợ lọc theo trạng thái hoạt động.")]
    [SwaggerResponse(200, "Danh sách nhân viên", typeof(ApiResponse<GetCompanyStaffResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    public async Task<IActionResult> GetStaffAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyStaffQuery(page, pageSize, isActive), ct)).ToHttp();

    [HttpPut("my/staff/{userId:guid}/status")]
    [Authorize(Roles = "CompanyManager")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CM] Vô hiệu hóa / kích hoạt lại nhân viên",
        Description = "CM toggle trạng thái IsActive của nhân viên. " +
            "Nhân viên bị deactivate không thể nhận task mới nhưng vẫn giữ record.")]
    [SwaggerResponse(200, "Đã cập nhật trạng thái", typeof(ApiResponse))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy nhân viên trong công ty", typeof(ApiResponse))]
    public async Task<IActionResult> ToggleStaffStatusAsync(
        [FromRoute] Guid userId, [FromBody] ToggleStaffStatusRequest request, CancellationToken ct)
        => (await sender.Send(new ToggleCompanyStaffStatusCommand(userId, request.IsActive), ct))
            .ToHttpNoContent("Đã cập nhật trạng thái nhân viên.");

    // ═══════════════════════════════════════════
    // ██  MY COMPANY PROFILE (CM Dashboard)
    // ═══════════════════════════════════════════

    [HttpGet("my")]
    [Authorize(Roles = "CompanyManager")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CM] Thông tin công ty của tôi",
        Description = "CompanyManager xem profile công ty mình đang quản lý (1 CM = 1 Company). " +
            "Trả về thông tin công ty, hợp đồng, dịch vụ area, số nhân sự.")]
    [SwaggerResponse(200, "Thông tin công ty", typeof(ApiResponse<CompanyDetailResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyCompanyAsync(CancellationToken ct)
        => (await sender.Send(new GetMyCompanyQuery(), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  CONTRACT RENEWAL (BR-CMP-006)
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/renew-contract")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Gia hạn / tái ký hợp đồng",
        Description = "Gia hạn hợp đồng cho công ty Bidding. " +
            "Tạo kỳ hợp đồng mới, cập nhật ContractEndDate. " +
            "Nếu công ty đang Expired → tự động kích hoạt lại (Active). " +
            "Subsidiary (vô thời hạn) không thể gia hạn.")]
    [SwaggerResponse(200, "Đã gia hạn", typeof(ApiResponse<RenewContractResponse>))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Subsidiary hoặc validation error", typeof(ApiResponse))]
    public async Task<IActionResult> RenewContractAsync(
        [FromRoute] Guid id, [FromBody] RenewContractRequest request, CancellationToken ct)
        => (await sender.Send(new RenewContractCommand(
            id, request.NewStartDate, request.NewEndDate,
            request.NewContractNumber, request.Note), ct)).ToHttp();

    [HttpGet("{id:guid}/contract-history")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Lịch sử kỳ hợp đồng",
        Description = "Trả về tất cả kỳ hợp đồng của công ty, sắp xếp mới nhất trước.")]
    [SwaggerResponse(200, "Lịch sử kỳ hợp đồng", typeof(ApiResponse<ContractHistoryResponse>))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> GetContractHistoryAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetContractHistoryQuery(id), ct)).ToHttp();

    [HttpGet("my/contract-history")]
    [Authorize(Roles = "CompanyManager")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CM] Lịch sử kỳ hợp đồng công ty tôi",
        Description = "CM xem lịch sử các kỳ hợp đồng của công ty mình.")]
    [SwaggerResponse(200, "Lịch sử kỳ hợp đồng", typeof(ApiResponse<ContractHistoryResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyContractHistoryAsync(CancellationToken ct)
        => (await sender.Send(new GetContractHistoryQuery(Guid.Empty), ct)).ToHttp();
    // Note: Guid.Empty signals handler to resolve from CM token — handled in GetContractHistoryQueryHandler

    // ═══════════════════════════════════════════
    // ██  KPI (BR-CMP-020)
    // ═══════════════════════════════════════════

    [HttpGet("{id:guid}/kpi")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] KPI công ty",
        Description = "Tính KPI cho công ty: số task tiếp nhận/hoàn thành, tỉ lệ đúng SLA, " +
            "thời gian xử lý trung bình. Hỗ trợ lọc theo khoảng thời gian hoặc preset (ThisMonth, LastQuarter...).")]
    [SwaggerResponse(200, "KPI công ty", typeof(ApiResponse<CompanyKpiResponse>))]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> GetCompanyKpiAsync(
        [FromRoute] Guid id,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        [FromQuery] string? period = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyKpiQuery(
            id, from, to, ParseKpiPeriod(period)), ct)).ToHttp();

    [HttpGet("my/kpi")]
    [Authorize(Roles = "CompanyManager")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CM] KPI công ty của tôi",
        Description = "CM xem KPI công ty mình. Tự động xác định companyId từ tài khoản.")]
    [SwaggerResponse(200, "KPI công ty", typeof(ApiResponse<CompanyKpiResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyCompanyKpiAsync(
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        [FromQuery] string? period = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyKpiQuery(
            null, from, to, ParseKpiPeriod(period)), ct)).ToHttp();

    // ── Helpers ──
    private static KpiPeriod? ParseKpiPeriod(string? period)
        => Enum.TryParse<KpiPeriod>(period, true, out var p) ? p : null;
}

// ── Request DTOs ──
public sealed record UpdateServiceAreasRequest(List<string> WardCodes);
public sealed record ToggleStaffStatusRequest(bool IsActive);
public sealed record CreateCompanyManagerRequest(string ManagerEmail, string ManagerFullName);
public sealed record CompanyStatusReasonRequest(string Reason);
public sealed record RenewContractRequest(
    DateTime NewStartDate,
    DateTime NewEndDate,
    string NewContractNumber,
    string? Note = null);


using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.CreateCompany;
using Greenlens.Application.Features.Organization.CreateCompanyStaff;
using Greenlens.Application.Features.Organization.GetCompanies;
using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Application.Features.Organization.GetCompanyServiceAreas;
using Greenlens.Application.Features.Organization.GetCompanyStaff;
using Greenlens.Application.Features.Organization.GetMyCompany;
using Greenlens.Application.Features.Organization.ToggleCompanyStaffStatus;
using Greenlens.Application.Features.Organization.UpdateCompanyServiceAreas;
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
        Summary = "[DEO/Admin] Tạo công ty DVMT + tài khoản CM",
        Description = "Tạo Công ty Dịch vụ Môi trường (trực thuộc/đấu thầu) + tài khoản CompanyManager. " +
            "Trạng thái ban đầu: PendingActivation. CM đăng nhập bằng MK tạm → đổi MK → công ty tự động Active. " +
            "⚠️ TempPassword chỉ hiển thị 1 lần — DEO cần gửi cho CM.")]
    [SwaggerResponse(201, "Đã tạo công ty + tài khoản CM", typeof(ApiResponse<CreateCompanyResponse>))]
    [SwaggerResponse(404, "Department không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(409, "Số hợp đồng hoặc email CM đã tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateCompanyCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

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
}

// ── Request DTOs ──
public sealed record UpdateServiceAreasRequest(List<string> WardCodes);
public sealed record ToggleStaffStatusRequest(bool IsActive);

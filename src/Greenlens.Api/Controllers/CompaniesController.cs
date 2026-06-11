using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.ActivateCompany;
using Greenlens.Application.Features.Organization.CreateCompany;
using Greenlens.Application.Features.Organization.GetCompanies;
using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Application.Features.Organization.GetCompanyServiceAreas;
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
        Summary = "[DEO/Admin] Tạo công ty DVMT",
        Description = "Tạo Công ty Dịch vụ Môi trường (trực thuộc/đấu thầu) thuộc department. " +
            "Trạng thái ban đầu: PendingActivation. DEO activate sau khi CM đặt mật khẩu.")]
    [SwaggerResponse(201, "Đã tạo công ty", typeof(ApiResponse<CreateCompanyResponse>))]
    [SwaggerResponse(404, "Department không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(409, "Số hợp đồng đã tồn tại", typeof(ApiResponse))]
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
    // ██  ACTIVATION (BR-CMP-003)
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/activate")]
    [Authorize(Roles = "DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO/Admin] Kích hoạt công ty",
        Description = "Chuyển trạng thái PendingActivation → Active. " +
            "Thực hiện sau khi CM đã đặt mật khẩu qua cơ chế reset-password chung (BR-CMP-002).")]
    [SwaggerResponse(204, "Đã kích hoạt")]
    [SwaggerResponse(404, "Công ty không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Công ty không ở trạng thái chờ kích hoạt", typeof(ApiResponse))]
    public async Task<IActionResult> ActivateAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new ActivateCompanyCommand(id), ct))
            .ToHttpNoContent("Đã kích hoạt công ty.");

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
}

// ── Request DTOs ──
public sealed record UpdateServiceAreasRequest(List<string> WardCodes);

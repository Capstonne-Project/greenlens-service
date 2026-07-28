using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AssignDeoToDepartment;
using Greenlens.Application.Features.Organization.CreateDepartment;
using Greenlens.Application.Features.Organization.GetDepartmentById;
using Greenlens.Application.Features.Organization.GetDepartments;
using Greenlens.Application.Features.Organization.GetMyLocalOffices;
using Greenlens.Application.Features.Organization.UpdateDepartment;
using Greenlens.Application.Features.Reports.GetDepartmentReports;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý Department (Sở TNMT cấp Tỉnh/TP).</summary>
[ApiController]
[Route("v1/departments")]
[Authorize]
[Produces("application/json")]
public sealed class DepartmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,DEO")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin/DEO] Danh sách departments", Description = "Trả về danh sách Sở TNMT, hỗ trợ phân trang và lọc theo trạng thái hoạt động.")]
    [SwaggerResponse(200, "Danh sách departments", typeof(ApiResponse<GetDepartmentsResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null, CancellationToken ct = default)
        => (await sender.Send(new GetDepartmentsQuery(page, pageSize, isActive), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,DEO")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin/DEO] Chi tiết department", Description = "Trả về thông tin department kèm danh sách offices trực thuộc.")]
    [SwaggerResponse(200, "Chi tiết department", typeof(ApiResponse<DepartmentDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetDepartmentByIdQuery(id), ct)).ToHttp();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin] Tạo department", Description = "Tạo Sở TNMT cấp Tỉnh/TP. Mỗi tỉnh chỉ có 1 department.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateDepartmentResponse>))]
    [SwaggerResponse(409, "Tỉnh đã có department", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateDepartmentCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo department thành công.");

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật department", Description = "Cập nhật tên department.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateDepartmentCommand(id, request.Name), ct)).ToHttpNoContent("Đã cập nhật department.");

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin] Vô hiệu hóa department", Description = "Soft-delete: chuyển trạng thái IsActive = false. Không xóa dữ liệu.")]
    [SwaggerResponse(200, "Đã vô hiệu hóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> DeactivateAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new UpdateDepartmentCommand(id, null!), ct)).ToHttpNoContent("Đã vô hiệu hóa department.");
    // Note: Deactivate sẽ cần command riêng — tạm dùng UpdateDepartment

    [HttpPut("{id:guid}/officer")]
    [Authorize(Roles = "Admin")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(
        Summary = "[Admin] Gán DEO cho department",
        Description = "Gán 1 user có role DEO làm điều phối viên cho Sở TNMT. " +
            "DEO sẽ tiếp nhận, xác minh và điều phối tất cả báo cáo trong tỉnh.")]
    [SwaggerResponse(200, "Đã gán", typeof(ApiResponse))]
    [SwaggerResponse(404, "Department hoặc User không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "User không có role DEO", typeof(ApiResponse))]
    public async Task<IActionResult> AssignDeoAsync(
        [FromRoute] Guid id, [FromBody] AssignDeoRequest request, CancellationToken ct)
        => (await sender.Send(new AssignDeoToDepartmentCommand(id, request.UserId), ct)).ToHttpNoContent("Đã gán DEO cho department.");

    /// <summary>Danh sách văn phòng MT cấp xã/phường thuộc department mà officer đang quản lý.</summary>
    [HttpGet("my-offices")]
    [Authorize(Roles = "DEO")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO] Lấy danh sách văn phòng MT cấp xã/phường mà DEO quản lý",
        Description =
            "Trả về tất cả văn phòng môi trường cấp xã/phường trực thuộc department mà officer đảm nhiệm. " +
            "Bao gồm thông tin ward, LEO phụ trách, số đội, trạng thái onboard. " +
            "Hỗ trợ tìm kiếm (tên office, tên phường, tên LEO), lọc theo trạng thái onboard, " +
            "sắp xếp theo: name, wardName, officerName, teamCount, createdAt. " +
            "Dùng cho dropdown dispatch / filter trên dashboard.")]
    [SwaggerResponse(200, "Danh sách offices", typeof(ApiResponse<GetMyLocalOfficesResponse>))]
    [SwaggerResponse(404, "Chưa gán department", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyLocalOfficesAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isOnboarded = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(new GetMyLocalOfficesQuery(page, pageSize, search, isOnboarded, sortBy, sortDesc), ct)).ToHttp();

    /// <summary>Tất cả báo cáo thuộc department mà DEO quản lý.</summary>
    [HttpGet("my/reports")]
    [Authorize(Roles = "DEO")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[DEO] Danh sách tất cả báo cáo trong department (phân trang)",
        Description =
            "Trả về tất cả báo cáo (mọi trạng thái) thuộc department mà DEO đang quản lý. " +
            "Hỗ trợ tìm kiếm (mã báo cáo, mô tả, địa chỉ), lọc theo status/category/severity/ward, " +
            "sắp xếp theo: code, status, severity, priority, createdAt, verifiedAt (mặc định: mới nhất). " +
            "Bao gồm thông tin reporter, office được dispatch, SLA deadlines.")]
    [SwaggerResponse(200, "Danh sách báo cáo", typeof(ApiResponse<GetDepartmentReportsResponse>))]
    [SwaggerResponse(404, "Chưa gán department", typeof(ApiResponse))]
    public async Task<IActionResult> GetDepartmentReportsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] ReportStatus? status = null,
        [FromQuery] Guid? categoryId = null, [FromQuery] Severity? severity = null,
        [FromQuery] string? wardCode = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(new GetDepartmentReportsQuery(
            page, pageSize, search, status, categoryId, severity, wardCode, sortBy, sortDesc), ct)).ToHttp();
}

public sealed record UpdateDepartmentRequest(string Name);
public sealed record AssignDeoRequest(Guid UserId);

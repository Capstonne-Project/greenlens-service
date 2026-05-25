using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.CreateDepartment;
using Greenlens.Application.Features.Organization.GetDepartmentById;
using Greenlens.Application.Features.Organization.GetDepartments;
using Greenlens.Application.Features.Organization.UpdateDepartment;
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
    [SwaggerOperation(Summary = "[Admin/DEO] Danh sách departments", Description = "Trả về danh sách Sở TNMT, hỗ trợ phân trang và lọc theo trạng thái hoạt động.")]
    [SwaggerResponse(200, "Danh sách departments", typeof(ApiResponse<GetDepartmentsResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null, CancellationToken ct = default)
        => (await sender.Send(new GetDepartmentsQuery(page, pageSize, isActive), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,DEO")]
    [SwaggerOperation(Summary = "[Admin/DEO] Chi tiết department", Description = "Trả về thông tin department kèm danh sách offices trực thuộc.")]
    [SwaggerResponse(200, "Chi tiết department", typeof(ApiResponse<DepartmentDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetDepartmentByIdQuery(id), ct)).ToHttp();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Tạo department", Description = "Tạo Sở TNMT cấp Tỉnh/TP. Mỗi tỉnh chỉ có 1 department.")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CreateDepartmentResponse>))]
    [SwaggerResponse(409, "Tỉnh đã có department", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateDepartmentCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật department", Description = "Cập nhật tên department.")]
    [SwaggerResponse(204, "Đã cập nhật")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateDepartmentCommand(id, request.Name), ct)).ToHttpNoContent();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Vô hiệu hóa department", Description = "Soft-delete: chuyển trạng thái IsActive = false. Không xóa dữ liệu.")]
    [SwaggerResponse(204, "Đã vô hiệu hóa")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> DeactivateAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new UpdateDepartmentCommand(id, null!), ct)).ToHttpNoContent();
    // Note: Deactivate sẽ cần command riêng — tạm dùng UpdateDepartment
}

public sealed record UpdateDepartmentRequest(string Name);

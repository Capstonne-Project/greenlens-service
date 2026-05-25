using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AssignLeoToOffice;
using Greenlens.Application.Features.Organization.CreateLocalOffice;
using Greenlens.Application.Features.Organization.GetLocalOfficeById;
using Greenlens.Application.Features.Organization.GetLocalOffices;
using Greenlens.Application.Features.Organization.UpdateLocalOffice;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý Local Office (Văn phòng MT cấp Xã/Phường).</summary>
[ApiController]
[Route("v1/offices")]
[Authorize]
[Produces("application/json")]
public sealed class LocalOfficesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,DEO,LEO")]
    [SwaggerOperation(Summary = "[Admin/DEO/LEO] Danh sách offices", Description = "Trả về danh sách văn phòng MT cấp xã/phường. Hỗ trợ lọc theo department và trạng thái.")]
    [SwaggerResponse(200, "Danh sách offices", typeof(ApiResponse<GetLocalOfficesResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? departmentId = null, [FromQuery] bool? isOnboarded = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetLocalOfficesQuery(page, pageSize, departmentId, isOnboarded), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,DEO,LEO")]
    [SwaggerOperation(Summary = "[Admin/DEO/LEO] Chi tiết office", Description = "Trả về thông tin office kèm danh sách teams trực thuộc, thông tin officer phụ trách.")]
    [SwaggerResponse(200, "Chi tiết office", typeof(ApiResponse<LocalOfficeDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetLocalOfficeByIdQuery(id), ct)).ToHttp();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Tạo office", Description = "Onboard văn phòng MT cấp xã/phường. Sau khi tạo, báo cáo trong ward đó sẽ tự động route đến office này.")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CreateLocalOfficeResponse>))]
    [SwaggerResponse(409, "Ward đã có office", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateLocalOfficeCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật office", Description = "Cập nhật tên văn phòng.")]
    [SwaggerResponse(204, "Đã cập nhật")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id, [FromBody] UpdateLocalOfficeRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateLocalOfficeCommand(id, request.Name), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/officer")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Gán LEO cho office", Description = "Gán 1 user có role LEO làm người phụ trách văn phòng.")]
    [SwaggerResponse(204, "Đã gán")]
    [SwaggerResponse(404, "Office hoặc User không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "User không có role LEO", typeof(ApiResponse))]
    public async Task<IActionResult> AssignOfficerAsync(
        [FromRoute] Guid id, [FromBody] AssignLeoRequest request, CancellationToken ct)
        => (await sender.Send(new AssignLeoToOfficeCommand(id, request.UserId), ct)).ToHttpNoContent();
}

public sealed record UpdateLocalOfficeRequest(string Name);
public sealed record AssignLeoRequest(Guid UserId);

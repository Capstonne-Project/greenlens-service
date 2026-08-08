using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Inspection.CreateViolatingEntity;
using Greenlens.Application.Features.Inspection.DeleteViolatingEntity;
using Greenlens.Application.Features.Inspection.GetViolatingEntityById;
using Greenlens.Application.Features.Inspection.SearchViolatingEntities;
using Greenlens.Application.Features.Inspection.UpdateViolatingEntity;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>
/// Quản lý đối tượng vi phạm (ViolatingEntity) — phục vụ biên bản hiện trường (BR-INS-010)
/// và phát hiện tái phạm (BR-INS-022).
/// </summary>
[ApiController]
[Route("v1/violating-entities")]
[Produces("application/json")]
[Authorize(Roles = "Inspector,LEO,Admin")]
public sealed class ViolatingEntitiesController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  SEARCH
    // ═══════════════════════════════════════════

    [HttpGet]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector/LEO] Tìm kiếm đối tượng vi phạm",
        Description = "Tìm kiếm theo MST (doanh nghiệp), CMND/CCCD (cá nhân), hoặc tên. " +
            "Kết quả kèm số lần bị lập biên bản trong 12 tháng (repeat offender indicator). " +
            "Ưu tiên: TaxCode > IdentityNumber > Name (partial match).")]
    [SwaggerResponse(200, "Danh sách đối tượng", typeof(ApiResponse<List<ViolatingEntityDto>>))]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? taxCode = null,
        [FromQuery] string? identityNumber = null,
        [FromQuery] string? name = null,
        [FromQuery] int maxResults = 20,
        CancellationToken ct = default)
        => (await sender.Send(
            new SearchViolatingEntitiesQuery(taxCode, identityNumber, name, maxResults), ct))
            .ToHttp();

    // ═══════════════════════════════════════════
    // ██  GET BY ID
    // ═══════════════════════════════════════════

    [HttpGet("{id:guid}")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector/LEO] Chi tiết đối tượng vi phạm",
        Description = "Xem thông tin đối tượng vi phạm + số lần bị lập biên bản trong 12 tháng.")]
    [SwaggerResponse(200, "Chi tiết đối tượng", typeof(ApiResponse<ViolatingEntityDto>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetViolatingEntityByIdQuery(id), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  CREATE
    // ═══════════════════════════════════════════

    [HttpPost]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Tạo đối tượng vi phạm mới",
        Description = "Tạo đối tượng vi phạm mới (BR-INS-010). " +
            "Doanh nghiệp bắt buộc MST. Cá nhân nên có CMND/CCCD. " +
            "Kiểm tra trùng MST / CMND/CCCD trước khi tạo.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<Guid>))]
    [SwaggerResponse(409, "MST hoặc CMND/CCCD đã tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateViolatingEntityRequest request,
        CancellationToken ct)
        => (await sender.Send(new CreateViolatingEntityCommand(
            request.Name,
            request.Type,
            request.Address,
            request.TaxCode,
            request.IdentityNumber,
            request.PhoneNumber), ct))
            .ToHttp("Đã tạo đối tượng vi phạm thành công.");

    // ═══════════════════════════════════════════
    // ██  UPDATE (PATCH)
    // ═══════════════════════════════════════════

    [HttpPatch("{id:guid}")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Cập nhật thông tin đối tượng vi phạm",
        Description = "Sửa thông tin đối tượng vi phạm (tên, địa chỉ, MST, CMND/CCCD, SĐT). " +
            "Chỉ các trường non-null được cập nhật (patch semantics). " +
            "Kiểm tra trùng MST / CMND/CCCD nếu thay đổi.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    [SwaggerResponse(409, "MST hoặc CMND/CCCD đã tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateViolatingEntityRequest request,
        CancellationToken ct)
        => (await sender.Send(new UpdateViolatingEntityCommand(
            id,
            request.Name,
            request.Address,
            request.TaxCode,
            request.IdentityNumber,
            request.PhoneNumber), ct))
            .ToHttpNoContent("Đã cập nhật thông tin đối tượng vi phạm.");

    [HttpDelete("{id:guid}")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(Summary = "[Inspector/LEO] Xóa đối tượng vi phạm (Soft Delete)", Description = "Xóa mềm đối tượng vi phạm. Các báo cáo liên quan sẽ vẫn giữ kết nối nhưng đối tượng sẽ không hiện lên danh sách tìm kiếm mới.")]
    [SwaggerResponse(200, "Đã xóa thành công", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy đối tượng", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteViolatingEntityCommand(id), ct)).ToHttpNoContent("Đã xóa đối tượng vi phạm.");
}

// ── Request DTOs ──
public sealed record CreateViolatingEntityRequest(
    string Name,
    ViolatorType Type,
    string? Address = null,
    string? TaxCode = null,
    string? IdentityNumber = null,
    string? PhoneNumber = null);

public sealed record UpdateViolatingEntityRequest(
    string? Name = null,
    string? Address = null,
    string? TaxCode = null,
    string? IdentityNumber = null,
    string? PhoneNumber = null);

using Greenlens.Api.Attributes;
using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AcceptInvitation;
using Greenlens.Application.Features.Organization.DeclineInvitation;
using Greenlens.Application.Features.Organization.GetMyInvitations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý lời mời tham gia đội cộng đồng (BR-ORG-020/021).</summary>
[ApiController]
[Route("v1/invitations")]
[Authorize]
[Produces("application/json")]
public sealed class InvitationsController(ISender sender) : ControllerBase
{
    [HttpGet("my")]
    [Tags("👤 Citizen")]
    [SwaggerOperation(
        Summary = "[Citizen] Xem lời mời của tôi",
        Description = "Trả về tất cả lời mời tham gia đội/phường, sắp theo mới nhất. " +
            "Bao gồm cả Pending, Accepted, Declined, Expired.")]
    [SwaggerResponse(200, "Danh sách lời mời", typeof(ApiResponse<List<InvitationDto>>))]
    public async Task<IActionResult> GetMyInvitationsAsync(CancellationToken ct)
        => (await sender.Send(new GetMyInvitationsQuery(), ct)).ToHttp();

    [HttpPost("{invitationId:guid}/accept")]
    [SupportsIdempotency]
    [Tags("👤 Citizen")]
    [SwaggerOperation(
        Summary = "[Citizen] Chấp nhận lời mời (BR-ORG-021)",
        Description = "Chấp nhận lời mời → đổi role thành Cleaner/Inspector, " +
            "gán vào LocalOffice + team (nếu có). Lời mời phải còn hạn (7 ngày) và chưa dùng.")]
    [SwaggerResponse(200, "Đã chấp nhận", typeof(ApiResponse<AcceptInvitationResponse>))]
    [SwaggerResponse(404, "Lời mời không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Lời mời hết hạn hoặc đã dùng", typeof(ApiResponse))]
    public async Task<IActionResult> AcceptAsync(Guid invitationId, CancellationToken ct)
        => (await sender.Send(new AcceptInvitationCommand(invitationId), ct)).ToHttp();

    [HttpPost("{invitationId:guid}/decline")]
    [Tags("👤 Citizen")]
    [SwaggerOperation(
        Summary = "[Citizen] Từ chối lời mời (BR-ORG-021)",
        Description = "Từ chối lời mời → giữ nguyên role Citizen. " +
            "LEO có thể gửi lại lời mời mới.")]
    [SwaggerResponse(200, "Đã từ chối", typeof(ApiResponse))]
    [SwaggerResponse(404, "Lời mời không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Lời mời hết hạn hoặc đã dùng", typeof(ApiResponse))]
    public async Task<IActionResult> DeclineAsync(Guid invitationId, CancellationToken ct)
        => (await sender.Send(new DeclineInvitationCommand(invitationId), ct)).ToHttpNoContent();
}

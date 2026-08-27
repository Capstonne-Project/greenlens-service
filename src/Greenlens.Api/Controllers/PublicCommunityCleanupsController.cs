using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Application.Features.CommunityCleanup.GetPublicCommunityCleanupPreview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>
/// Anonymous community-cleanup previews for Next.js OG landing pages and social crawlers.
/// </summary>
[ApiController]
[Route("v1/public/community-cleanups")]
[Produces("application/json")]
[Tags("🌐 Public — Community Cleanup Share")]
public sealed class PublicCommunityCleanupsController(ISender sender) : ControllerBase
{
    [HttpGet("{eventId:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "[Public] Preview chương trình dọn cộng đồng (OG / share)",
        Description = "Không cần đăng nhập. Dùng cho Next.js generateMetadata (Facebook OG) và dialog chia sẻ sau khi LEO tạo chương trình. " +
                      "Trả về title, mô tả, thumbnail, share URLs. Chương trình Cancelled → 404.")]
    [SwaggerResponse(200, "Preview", typeof(ApiResponse<CommunityCleanupPublicPreviewResponse>))]
    [SwaggerResponse(404, "Không tìm thấy hoặc đã hủy", typeof(ApiResponse))]
    public async Task<IActionResult> GetPreviewAsync([FromRoute] Guid eventId, CancellationToken ct)
        => (await sender.Send(new GetPublicCommunityCleanupPreviewQuery(eventId), ct)).ToHttp();
}

using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Gamification.GetBadgeCatalog;
using Greenlens.Application.Features.Gamification.GetLeaderboard;
using Greenlens.Application.Features.Gamification.GetMyBadges;
using Greenlens.Application.Features.Gamification.GetMyPoints;
using Greenlens.Application.Features.Gamification.LockGamification;
using Greenlens.Application.Features.Gamification.SetFeaturedBadge;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/gamification")]
[Produces("application/json")]
[Tags("🏆 Gamification — Points, Badges & Leaderboard")]
public sealed class GamificationController(ISender sender) : ControllerBase
{
    /// <summary>View my gamification points, level, and recent transactions.</summary>
    [HttpGet("my-points")]
    [Authorize]
    [SwaggerOperation(Summary = "Get my points & level")]
    [SwaggerResponse(200, "Points data", typeof(ApiResponse<MyPointsResponse>))]
    public async Task<IActionResult> GetMyPoints(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return (await sender.Send(new GetMyPointsQuery(userId, page, pageSize), ct)).ToHttp();
    }

    /// <summary>View my earned badges.</summary>
    [HttpGet("my-badges")]
    [Authorize]
    [SwaggerOperation(Summary = "Get my badges")]
    [SwaggerResponse(200, "Badge list", typeof(ApiResponse<IReadOnlyList<BadgeItem>>))]
    public async Task<IActionResult> GetMyBadges(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return (await sender.Send(new GetMyBadgesQuery(userId), ct)).ToHttp();
    }

    /// <summary>Full badge catalog — earned + locked, to motivate progress.</summary>
    [HttpGet("badges")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get full badge catalog (earned + locked)",
        Description = "Mỗi badge trả currentProgressValue + targetProgressValue + progressMetric để FE hiển thị tiến trình (vd. 3/5 duplicate_reports). " +
            "progressMetric: verified_reports | points | streak_days | duplicate_reports | cleanup_events | reporter_count.")]
    [SwaggerResponse(200, "Badge catalog", typeof(ApiResponse<IReadOnlyList<BadgeCatalogItem>>))]
    public async Task<IActionResult> GetBadgeCatalog(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return (await sender.Send(new GetBadgeCatalogQuery(userId), ct)).ToHttp();
    }

    /// <summary>Chọn (hoặc bỏ) huy hiệu hiển thị nổi bật trên hồ sơ. Truyền badgeId = null để bỏ.</summary>
    [HttpPut("featured-badge")]
    [Authorize]
    [SwaggerOperation(Summary = "Set featured badge shown on profile")]
    [SwaggerResponse(200, "Featured badge updated", typeof(ApiResponse<SetFeaturedBadgeResponse>))]
    public async Task<IActionResult> SetFeaturedBadge(
        [FromBody] SetFeaturedBadgeRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return (await sender.Send(new SetFeaturedBadgeCommand(userId, request.BadgeId), ct)).ToHttp();
    }

    /// <summary>Public leaderboard — top users by period.</summary>
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get leaderboard")]
    [SwaggerResponse(200, "Leaderboard", typeof(ApiResponse<LeaderboardResponse>))]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] LeaderboardPeriod period = LeaderboardPeriod.AllTime,
        [FromQuery] int top = 10,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken ct = default)
    {
        return (await sender.Send(new GetLeaderboardQuery(period, top, year, month), ct)).ToHttp();
    }

    /// <summary>Admin: Lock a user's gamification for fraud (BR-GAM-006).</summary>
    [HttpPost("{userId:guid}/lock")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Lock gamification — fraud penalty")]
    [SwaggerResponse(200, "Lock result", typeof(ApiResponse<LockGamificationResponse>))]
    public async Task<IActionResult> LockGamification(
        Guid userId,
        [FromBody] LockGamificationRequest request,
        CancellationToken ct)
    {
        return (await sender.Send(
            new LockGamificationCommand(userId, request.Reason, request.LockDays), ct)).ToHttp();
    }
}

public sealed record LockGamificationRequest(string Reason, int LockDays = 30);

public sealed record SetFeaturedBadgeRequest(Guid? BadgeId);

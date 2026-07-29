using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Users;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.GetProfile;

/// <summary>
/// Get the authenticated user's own profile.
/// </summary>
/// <remarks>Implements: BR-GAM-004 (badges), BR-GAM-005 (all-time rank).</remarks>
public sealed class GetProfileQueryHandler(
    IUserRepository users,
    IUserBadgeRepository userBadges,
    IUserPointsRepository userPoints,
    IBadgeRepository badges,
    ICurrentUser currentUser,
    ILogger<GetProfileQueryHandler> logger)
    : IRequestHandler<GetProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting profile for user {UserId}", currentUser.UserId);

        var user = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.AvatarUrl,
                u.Role,
                u.IsEmailVerified,
                u.GoogleId,
                u.CreatedAt,
                u.UpdatedAt,
                u.FeaturedBadgeId
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("Profile not found for user {UserId}", currentUser.UserId);
            return Result<UserProfileDto>.Failure(Errors.Users.UserNotFound);
        }

        var achievements = await userBadges.QueryAsNoTracking()
            .Where(ub => ub.UserId == currentUser.UserId)
            .OrderByDescending(ub => ub.AwardedAt)
            .Select(ub => ub.Badge!.NameVi)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rank = await ResolveAllTimeRankAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        FeaturedBadgeDto? featuredBadge = null;
        if (user.FeaturedBadgeId is not null)
        {
            var badge = await badges.GetByIdAsync(user.FeaturedBadgeId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (badge is not null)
                featuredBadge = new FeaturedBadgeDto(badge.Id, badge.NameVi, badge.NameEn, badge.IconUrl);
        }

        logger.LogInformation("Lấy thông tin chi tiết người dùng thành công. ID: {UserId}", currentUser.UserId);

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.Role,
            user.IsEmailVerified,
            user.GoogleId,
            user.CreatedAt,
            user.UpdatedAt,
            achievements,
            rank,
            featuredBadge);
    }

    private async Task<int?> ResolveAllTimeRankAsync(Guid userId, CancellationToken ct)
    {
        var userStats = await userPoints.QueryAsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => new
            {
                up.IsLocked,
                up.TotalPoints
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (userStats is null or { IsLocked: true } or { TotalPoints: <= 0 })
            return null;

        var higherCount = await userPoints.QueryAsNoTracking()
            .Where(up => !up.IsLocked && up.TotalPoints > userStats.TotalPoints)
            .CountAsync(ct)
            .ConfigureAwait(false);

        return higherCount + 1;
    }
}

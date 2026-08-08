using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification;
using Greenlens.Application.Features.Users;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.GetProfile;

/// <summary>
/// Get the authenticated user's own profile.
/// </summary>
/// <remarks>Implements: BR-GAM-001, BR-GAM-003, BR-GAM-004, BR-GAM-005.</remarks>
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

        var gamification = await userPoints.QueryAsNoTracking()
            .Where(up => up.UserId == currentUser.UserId)
            .Select(up => new { up.TotalPoints, up.IsLocked })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalPoints = gamification?.TotalPoints ?? 0;
        var level = GamificationHelpers.CalculateLevel(totalPoints);
        var isGamificationLocked = gamification?.IsLocked ?? false;

        int? rank = !isGamificationLocked && totalPoints > 0
            ? await ResolveAllTimeRankAsync(totalPoints, cancellationToken).ConfigureAwait(false)
            : null;

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
            totalPoints,
            level,
            isGamificationLocked,
            rank,
            featuredBadge);
    }

    /// <summary>BR-GAM-005: hạng = số người có điểm cao hơn + 1, bỏ qua tài khoản bị khóa điểm.</summary>
    private async Task<int?> ResolveAllTimeRankAsync(int totalPoints, CancellationToken ct)
    {
        var higherCount = await userPoints.QueryAsNoTracking()
            .Where(up => !up.IsLocked && up.TotalPoints > totalPoints)
            .CountAsync(ct)
            .ConfigureAwait(false);

        return higherCount + 1;
    }
}

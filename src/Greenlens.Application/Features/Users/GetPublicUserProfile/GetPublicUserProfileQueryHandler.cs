using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.GetPublicUserProfile;

/// <summary>
/// Get another user's public profile — mở khi bấm vào tên/avatar người gửi báo cáo
/// hoặc tác giả bình luận.
/// </summary>
/// <remarks>
/// Implements: BR-GAM-003 (level), BR-GAM-004 (featured badge), BR-GAM-005 (all-time rank),
/// BR-GAM-006 (điểm bị khóa thì không công khai), BR-DAT-002 (không lộ PII).
///
/// Không trả Email/PhoneNumber/GoogleId. Người dùng đã xóa tài khoản bị global
/// query filter của <c>SoftDeletableEntity</c> loại bỏ → trả về NotFound (BR-AUTH-022).
/// Nhân viên đội xử lý (CompanyStaff/Cleaner) hiển thị nhãn chung thay tên thật,
/// đồng bộ với <see cref="CommentAccess.ResolveAuthorDisplayName"/> (BR-CMT-001).
/// </remarks>
public sealed class GetPublicUserProfileQueryHandler(
    IUserRepository users,
    IUserBadgeRepository userBadges,
    IUserPointsRepository userPoints,
    IBadgeRepository badges,
    IReportRepository reports,
    ILogger<GetPublicUserProfileQueryHandler> logger)
    : IRequestHandler<GetPublicUserProfileQuery, Result<PublicUserProfileDto>>
{
    public async Task<Result<PublicUserProfileDto>> Handle(
        GetPublicUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting public profile for user {UserId}", request.UserId);

        var user = await users.QueryAsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.AvatarUrl,
                u.Role,
                u.IsBanned,
                u.CreatedAt,
                u.FeaturedBadgeId
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("Public profile not found for user {UserId}", request.UserId);
            return Result<PublicUserProfileDto>.Failure(Errors.Users.UserNotFound);
        }

        // BR-AUTH-015: tài khoản bị ban không còn hiện diện công khai.
        if (user.IsBanned)
        {
            logger.LogWarning("Public profile requested for banned user {UserId}", request.UserId);
            return Result<PublicUserProfileDto>.Failure(Errors.Users.UserNotFound);
        }

        var stats = await userPoints.QueryAsNoTracking()
            .Where(up => up.UserId == request.UserId)
            // Level là computed property (BR-GAM-003) — EF không dịch được, tính sau khi materialize.
            .Select(up => new { up.IsLocked, up.TotalPoints })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // BR-GAM-006: điểm bị khóa (nghi gian lận) không công khai.
        // Chưa có bản ghi điểm (tài khoản mới) vẫn công khai — hiển thị 0 điểm.
        var pointsLocked = stats is { IsLocked: true };
        var pointsVisible = !pointsLocked;
        var totalPoints = stats?.TotalPoints ?? 0;

        int? rank = pointsVisible && totalPoints > 0
            ? await ResolveAllTimeRankAsync(totalPoints, cancellationToken).ConfigureAwait(false)
            : null;

        var achievements = pointsVisible
            ? await userBadges.QueryAsNoTracking()
                .Where(ub => ub.UserId == request.UserId)
                .OrderByDescending(ub => ub.AwardedAt)
                .Select(ub => ub.Badge!.NameVi)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : [];

        // Chỉ đếm báo cáo công khai: bỏ báo cáo ẩn (BR-ADM-006) và ẩn danh (BR-REP-012).
        var reportCount = await reports.QueryAsNoTracking()
            .CountAsync(
                r => r.ReporterId == request.UserId && !r.IsHidden && !r.HideReporterName,
                cancellationToken)
            .ConfigureAwait(false);

        FeaturedBadgeDto? featuredBadge = null;
        if (pointsVisible && user.FeaturedBadgeId is not null)
        {
            var badge = await badges.GetByIdAsync(user.FeaturedBadgeId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (badge is not null)
                featuredBadge = new FeaturedBadgeDto(badge.Id, badge.NameVi, badge.NameEn, badge.IconUrl);
        }

        var displayName = CommentAccess.ResolveAuthorDisplayName(user.Role.ToString(), user.FullName);

        logger.LogInformation("Lấy hồ sơ công khai thành công. ID: {UserId}", request.UserId);

        return new PublicUserProfileDto(
            user.Id,
            displayName,
            user.AvatarUrl,
            user.Role,
            pointsVisible ? totalPoints : null,
            pointsVisible ? ResolveLevel(totalPoints) : null,
            rank,
            reportCount,
            achievements,
            featuredBadge,
            user.CreatedAt);
    }

    /// <summary>
    /// BR-GAM-003: L1 (0–99), L2 (100–499), L3 (500–1499), L4 (1500–4999), L5 (≥5000).
    /// Giữ đồng bộ với <c>UserPoints.Level</c> — không dịch được sang SQL nên tính ở đây.
    /// </summary>
    private static int ResolveLevel(int totalPoints) => totalPoints switch
    {
        >= 5000 => 5,
        >= 1500 => 4,
        >= 500 => 3,
        >= 100 => 2,
        _ => 1
    };

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

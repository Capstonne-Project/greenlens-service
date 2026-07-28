using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.CheckBadges;

/// <summary>
/// Checks all active badges and awards any that the user qualifies for but hasn't earned yet.
/// Typically triggered after AwardPoints.
/// </summary>
/// <remarks>Implements: BR-GAM-004, BR-NTF-002 (BadgeEarned notification).</remarks>
public sealed record CheckBadgesCommand(Guid UserId) : IRequest<Result<CheckBadgesResponse>>, INoTransaction;

public sealed record CheckBadgesResponse(IReadOnlyList<string> NewlyAwarded);

public sealed class CheckBadgesCommandHandler(
    IUserPointsRepository userPointsRepo,
    IBadgeRepository badgeRepo,
    IUserBadgeRepository userBadgeRepo,
    IReportRepository reportRepo,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IChangeTrackerCleaner changeTrackerCleaner,
    ILogger<CheckBadgesCommandHandler> logger)
    : IRequestHandler<CheckBadgesCommand, Result<CheckBadgesResponse>>
{
    public async Task<Result<CheckBadgesResponse>> Handle(
        CheckBadgesCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting check badges");

        changeTrackerCleaner.ClearTrackedEntities();

        var userPoints = await userPointsRepo
            .GetByUserIdAsync(request.UserId, ct)
            .ConfigureAwait(false);

        var totalPoints = userPoints?.TotalPoints ?? 0;
        var metrics = await LoadMetricsAsync(request.UserId, ct).ConfigureAwait(false);

        var allBadges = await badgeRepo.GetAllActiveAsync(ct).ConfigureAwait(false);
        var earnedBadgeIds = (await userBadgeRepo
            .GetByUserIdAsync(request.UserId, ct)
            .ConfigureAwait(false))
            .Select(ub => ub.BadgeId)
            .ToHashSet();

        var newlyAwardedCodes = new List<string>();
        var newlyAwardedBadges = new List<Badge>();

        foreach (var badge in allBadges)
        {
            if (earnedBadgeIds.Contains(badge.Id))
            {
                logger.LogDebug("Badge {BadgeId} already awarded to user {UserId}", badge.Id, request.UserId);
                continue;
            }

            if (!BadgeEligibilityEvaluator.IsEligible(badge, totalPoints, metrics))
            {
                logger.LogDebug("Badge {BadgeCode} not eligible for user {UserId}", badge.Code, request.UserId);
                continue;
            }

            var userBadge = UserBadge.Create(request.UserId, badge.Id);
            userBadgeRepo.Add(userBadge);
            newlyAwardedCodes.Add(badge.Code);
            newlyAwardedBadges.Add(badge);

            logger.LogInformation(
                "Badge '{BadgeCode}' awarded to user {UserId}",
                badge.Code, request.UserId);
        }

        if (newlyAwardedBadges.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            foreach (var badge in newlyAwardedBadges)
            {
                await notificationService.SendFromTemplateAsync(
                    request.UserId,
                    NotificationType.BadgeEarned,
                    GamificationNotificationPlaceholders.ForBadgeEarned(badge),
                    referenceId: badge.Id,
                    ct).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Check badges completed: {NewlyAwarded}", newlyAwardedCodes);

        return new CheckBadgesResponse(newlyAwardedCodes);
    }

    private async Task<BadgeEligibilityMetrics> LoadMetricsAsync(Guid userId, CancellationToken ct)
    {
        var userReports = reportRepo.QueryAsNoTracking()
            .Where(r => r.ReporterId == userId);

        var verifiedReportCount = await userReports
            .CountAsync(r => r.Status != ReportStatus.Rejected
                             && r.Status != ReportStatus.Submitted, ct)
            .ConfigureAwait(false);

        var duplicateReportCount = await userReports
            .CountAsync(r => r.Status == ReportStatus.Duplicate, ct)
            .ConfigureAwait(false);

        var hasCommunityVoice = await userReports
            .AnyAsync(r => r.ReporterCount >= 10, ct)
            .ConfigureAwait(false);

        var submitTimestamps = await userReports
            .Select(r => r.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var maxSubmitStreakDays = ReportStreakCalculator.ComputeMaxConsecutiveDays(submitTimestamps);

        return new BadgeEligibilityMetrics(
            verifiedReportCount,
            duplicateReportCount,
            hasCommunityVoice,
            maxSubmitStreakDays);
    }
}

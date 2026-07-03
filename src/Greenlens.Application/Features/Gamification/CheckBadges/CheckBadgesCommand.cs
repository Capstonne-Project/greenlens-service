using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.CheckBadges;

/// <summary>
/// Checks all active badges and awards any that the user qualifies for but hasn't earned yet.
/// Typically triggered after AwardPoints.
/// </summary>
/// <remarks>Implements: BR-GAM-004.</remarks>
public sealed record CheckBadgesCommand(Guid UserId) : IRequest<Result<CheckBadgesResponse>>, INoTransaction;

public sealed record CheckBadgesResponse(IReadOnlyList<string> NewlyAwarded);

public sealed class CheckBadgesCommandHandler(
    IUserPointsRepository userPointsRepo,
    IBadgeRepository badgeRepo,
    IUserBadgeRepository userBadgeRepo,
    IReportRepository reportRepo,
    IUnitOfWork unitOfWork,
    ILogger<CheckBadgesCommandHandler> logger)
    : IRequestHandler<CheckBadgesCommand, Result<CheckBadgesResponse>>
{
    public async Task<Result<CheckBadgesResponse>> Handle(
        CheckBadgesCommand request, CancellationToken ct)
    {
        var userPoints = await userPointsRepo
            .GetByUserIdAsync(request.UserId, ct)
            .ConfigureAwait(false);

        var totalPoints = userPoints?.TotalPoints ?? 0;

        // Count verified reports by this user
        var verifiedReportCount = await reportRepo.QueryAsNoTracking()
            .CountAsync(r => r.ReporterId == request.UserId
                && r.Status != Domain.Enums.ReportStatus.Rejected
                && r.Status != Domain.Enums.ReportStatus.Submitted, ct)
            .ConfigureAwait(false);

        var allBadges = await badgeRepo.GetAllActiveAsync(ct).ConfigureAwait(false);
        var earnedBadgeIds = (await userBadgeRepo
            .GetByUserIdAsync(request.UserId, ct)
            .ConfigureAwait(false))
            .Select(ub => ub.BadgeId)
            .ToHashSet();

        var newlyAwarded = new List<string>();

        foreach (var badge in allBadges)
        {
            if (earnedBadgeIds.Contains(badge.Id))
                continue;

            if (!IsEligible(badge, totalPoints, verifiedReportCount))
                continue;

            var userBadge = UserBadge.Create(request.UserId, badge.Id);
            userBadgeRepo.Add(userBadge);
            newlyAwarded.Add(badge.Code);

            logger.LogInformation(
                "Badge '{BadgeCode}' awarded to user {UserId}",
                badge.Code, request.UserId);
        }

        if (newlyAwarded.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new CheckBadgesResponse(newlyAwarded);
    }

    private static bool IsEligible(Badge badge, int totalPoints, int reportCount)
    {
        return badge.Code switch
        {
            "first_report" => reportCount >= 1,
            "eco_warrior" => reportCount >= 10,
            // "hotspot_hunter" — TODO: enable when BR-MAP-010 hotspot detection is implemented
            "hotspot_hunter" => false,
            "streak_7d" => false, // TODO: need consecutive-day tracking
            // Point-threshold badges
            _ => badge.RequiredPoints.HasValue && totalPoints >= badge.RequiredPoints.Value
                || badge.RequiredReportCount.HasValue && reportCount >= badge.RequiredReportCount.Value
        };
    }
}

using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Gamification.GetBadgeCatalog;

/// <remarks>Implements: BR-GAM-004.</remarks>
public sealed record GetBadgeCatalogQuery(Guid UserId) : IRequest<Result<IReadOnlyList<BadgeCatalogItem>>>;

public sealed record BadgeCatalogItem(
    Guid BadgeId, string Code, string NameVi, string NameEn,
    string? Description, string? IconUrl,
    bool IsUnlocked, DateTime? AwardedAt,
    int? RequiredPoints, int? RequiredReportCount, int? RequiredStreakDays,
    bool IsFeatured,
    int? CurrentProgressValue);

public sealed class GetBadgeCatalogQueryHandler(
    IBadgeRepository badgeRepo,
    IUserBadgeRepository userBadgeRepo,
    IUserRepository userRepo,
    IUserPointsRepository userPointsRepo,
    IReportRepository reportRepo,
    IApplicationDbContext db)
    : IRequestHandler<GetBadgeCatalogQuery, Result<IReadOnlyList<BadgeCatalogItem>>>
{
    public async Task<Result<IReadOnlyList<BadgeCatalogItem>>> Handle(
        GetBadgeCatalogQuery request, CancellationToken ct)
    {
        var allBadges = await badgeRepo.GetAllActiveAsync(ct).ConfigureAwait(false);
        var ownedBadges = await userBadgeRepo.GetByUserIdAsync(request.UserId, ct).ConfigureAwait(false);
        var ownedByBadgeId = ownedBadges.ToDictionary(ub => ub.BadgeId, ub => ub.AwardedAt);

        var user = await userRepo.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        var featuredBadgeId = user?.FeaturedBadgeId;

        var (totalPoints, metrics) = await BadgeMetricsProvider
            .LoadAsync(request.UserId, userPointsRepo, reportRepo, db, ct)
            .ConfigureAwait(false);

        var items = allBadges
            .Select(b =>
            {
                var isUnlocked = ownedByBadgeId.ContainsKey(b.Id);
                return new BadgeCatalogItem(
                    b.Id, b.Code, b.NameVi, b.NameEn, b.Description, b.IconUrl,
                    IsUnlocked: isUnlocked,
                    AwardedAt: ownedByBadgeId.TryGetValue(b.Id, out var awardedAt) ? awardedAt : null,
                    b.RequiredPoints, b.RequiredReportCount, b.RequiredStreakDays,
                    IsFeatured: featuredBadgeId == b.Id,
                    CurrentProgressValue: isUnlocked
                        ? null
                        : BadgeEligibilityEvaluator.GetCurrentProgressValue(b, totalPoints, metrics));
            })
            .OrderByDescending(i => i.IsUnlocked)
            .ThenBy(i => i.RequiredPoints ?? i.RequiredReportCount ?? int.MaxValue)
            .ToList();

        return items;
    }
}

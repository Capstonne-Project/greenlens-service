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
    int? RequiredPoints, int? RequiredReportCount,
    bool IsFeatured);

public sealed class GetBadgeCatalogQueryHandler(
    IBadgeRepository badgeRepo,
    IUserBadgeRepository userBadgeRepo,
    IUserRepository userRepo)
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

        var items = allBadges
            .Select(b => new BadgeCatalogItem(
                b.Id, b.Code, b.NameVi, b.NameEn, b.Description, b.IconUrl,
                IsUnlocked: ownedByBadgeId.ContainsKey(b.Id),
                AwardedAt: ownedByBadgeId.TryGetValue(b.Id, out var awardedAt) ? awardedAt : null,
                b.RequiredPoints, b.RequiredReportCount,
                IsFeatured: featuredBadgeId == b.Id))
            .OrderByDescending(i => i.IsUnlocked)
            .ThenBy(i => i.RequiredPoints ?? i.RequiredReportCount ?? int.MaxValue)
            .ToList();

        return items;
    }
}

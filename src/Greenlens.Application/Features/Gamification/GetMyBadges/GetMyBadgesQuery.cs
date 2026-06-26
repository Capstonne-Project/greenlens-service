using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Gamification.GetMyBadges;

/// <remarks>Implements: BR-GAM-004.</remarks>
public sealed record GetMyBadgesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<BadgeItem>>>;

public sealed record BadgeItem(
    Guid BadgeId, string Code, string NameVi, string NameEn,
    string? Description, string? IconUrl, DateTime AwardedAt);

public sealed class GetMyBadgesQueryHandler(
    IUserBadgeRepository userBadgeRepo)
    : IRequestHandler<GetMyBadgesQuery, Result<IReadOnlyList<BadgeItem>>>
{
    public async Task<Result<IReadOnlyList<BadgeItem>>> Handle(
        GetMyBadgesQuery request, CancellationToken ct)
    {
        var badges = await userBadgeRepo.QueryAsNoTracking()
            .Where(ub => ub.UserId == request.UserId)
            .Include(ub => ub.Badge)
            .OrderByDescending(ub => ub.AwardedAt)
            .Select(ub => new BadgeItem(
                ub.BadgeId, ub.Badge!.Code, ub.Badge.NameVi, ub.Badge.NameEn,
                ub.Badge.Description, ub.Badge.IconUrl, ub.AwardedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return badges;
    }
}

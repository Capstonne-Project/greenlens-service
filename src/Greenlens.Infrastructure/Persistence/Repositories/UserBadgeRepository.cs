using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class UserBadgeRepository(ApplicationDbContext db)
    : GenericRepository<UserBadge>(db), IUserBadgeRepository
{
    public async Task<List<UserBadge>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.UserBadges
            .AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Badge)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasBadgeAsync(Guid userId, Guid badgeId, CancellationToken ct = default)
    {
        return await Context.UserBadges
            .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId, ct)
            .ConfigureAwait(false);
    }

    public async Task<HashSet<string>> GetEarnedBadgeCodesAsync(Guid userId, CancellationToken ct = default)
    {
        var codes = await Context.UserBadges
            .AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.Badge!.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

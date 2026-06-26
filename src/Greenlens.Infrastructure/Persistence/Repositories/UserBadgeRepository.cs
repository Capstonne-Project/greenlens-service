using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class UserBadgeRepository(ApplicationDbContext db)
    : GenericRepository<UserBadge>(db), IUserBadgeRepository
{
    public async Task<List<UserBadge>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.UserBadges
            .AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Badge)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasBadgeAsync(Guid userId, Guid badgeId, CancellationToken ct = default)
    {
        return await db.UserBadges
            .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId, ct)
            .ConfigureAwait(false);
    }
}

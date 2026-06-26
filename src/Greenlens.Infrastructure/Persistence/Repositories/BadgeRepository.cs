using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class BadgeRepository(ApplicationDbContext db)
    : GenericRepository<Badge>(db), IBadgeRepository
{
    public async Task<List<Badge>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await db.Badges
            .AsNoTracking()
            .Where(b => b.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}

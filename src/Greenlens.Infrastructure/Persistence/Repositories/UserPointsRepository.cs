using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class UserPointsRepository(ApplicationDbContext db)
    : GenericRepository<UserPoints>(db), IUserPointsRepository
{
    public async Task<UserPoints> GetOrCreateByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await db.UserPoints
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            return existing;

        var userPoints = UserPoints.Create(userId);
        db.UserPoints.Add(userPoints);
        return userPoints;
    }

    public async Task<UserPoints?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.UserPoints
            .AsNoTracking()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            .ConfigureAwait(false);
    }
}

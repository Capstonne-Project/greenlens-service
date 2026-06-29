using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class PasswordHistoryRepository(ApplicationDbContext db) : IPasswordHistoryRepository
{
    public async Task<List<PasswordHistory>> GetRecentAsync(Guid userId, int count, CancellationToken ct = default)
        => await db.PasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(count)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public void Add(PasswordHistory entry) => db.PasswordHistories.Add(entry);
}

using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class BlockedWordRepository(ApplicationDbContext db)
    : GenericRepository<BlockedWord>(db), IBlockedWordRepository
{
    public Task<bool> ExistsWordAsync(string normalizedWord, Guid? excludeId, CancellationToken ct) =>
        db.BlockedWords.AsNoTracking()
            .AnyAsync(
                w => w.Word == normalizedWord && (excludeId == null || w.Id != excludeId),
                ct);
}

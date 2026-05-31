using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class WasteTagRepository(ApplicationDbContext context)
    : GenericRepository<WasteTag>(context), IWasteTagRepository
{
    public async Task<List<WasteTag>> GetAllActiveAsync(CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<List<WasteTag>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default) =>
        await DbSet.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class WasteTagRepository(ApplicationDbContext context)
    : GenericRepository<WasteTag>(context), IWasteTagRepository
{
    public Task<bool> CodeExistsAsync(string code, Guid? excludeTagId = null, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = Context.WasteTags
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.Code == normalized);

        if (excludeTagId.HasValue)
            query = query.Where(t => t.Id != excludeTagId.Value);

        return query.AnyAsync(ct);
    }

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

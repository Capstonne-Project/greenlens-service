using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class PollutionCategoryRepository(ApplicationDbContext context)
    : GenericRepository<PollutionCategory>(context), IPollutionCategoryRepository
{
    public Task<bool> CodeExistsAsync(string code, Guid? excludeCategoryId = null, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = Context.PollutionCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Code == normalized);

        if (excludeCategoryId.HasValue)
            query = query.Where(c => c.Id != excludeCategoryId.Value);

        return query.AnyAsync(ct);
    }

    public Task<bool> ExistsActiveAsync(Guid id, CancellationToken ct = default) =>
        DbSet.AnyAsync(c => c.Id == id && c.IsActive, ct);

    public Task<PollutionCategory?> GetActiveByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return DbSet.FirstOrDefaultAsync(
            c => c.Code == normalized && c.IsActive,
            ct);
    }
}

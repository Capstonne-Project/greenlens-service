using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class LocalOfficeRepository(ApplicationDbContext db)
    : GenericRepository<LocalOffice>(db), ILocalOfficeRepository
{
    public Task<bool> ExistsByWardCodeAsync(string wardCode, CancellationToken ct = default)
        => QueryAsNoTracking()
            .AnyAsync(lo => lo.WardCode == wardCode, ct);

    /// <inheritdoc />
    public IQueryable<LocalOffice> ApplySearchTokens(IQueryable<LocalOffice> query, IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            var pattern = VietnameseTextSearch.ToContainsPattern(token);
            // ILike (PG) — không phụ thuộc C# ToLower vs SQL LOWER (Đ/đ).
            query = query.Where(o =>
                EF.Functions.ILike(o.Name, pattern) ||
                (o.Ward != null && EF.Functions.ILike(o.Ward.Name, pattern)) ||
                (o.Officer != null && EF.Functions.ILike(o.Officer.FullName, pattern)));
        }

        return query;
    }
}

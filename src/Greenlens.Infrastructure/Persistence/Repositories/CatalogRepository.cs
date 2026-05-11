using System.Linq.Expressions;
using Greenlens.Application.Common.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF-backed catalog queries for entities without <see cref="Greenlens.Domain.Common.BaseEntity"/>.
/// </summary>
internal abstract class CatalogRepository<T>(ApplicationDbContext context)
    : ICatalogRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public IQueryable<T> Query() => DbSet;

    public IQueryable<T> QueryAsNoTracking() => DbSet.AsNoTracking();

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AnyAsync(predicate, ct).ConfigureAwait(false);
}

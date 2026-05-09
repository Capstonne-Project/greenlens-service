using System.Linq.Expressions;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal abstract class GenericRepository<T>(ApplicationDbContext context)
    : IGenericRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public IQueryable<T> Query() => DbSet;

    public IQueryable<T> QueryAsNoTracking() => DbSet.AsNoTracking();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct).ConfigureAwait(false);

    public void Add(T entity) => DbSet.Add(entity);

    public void AddRange(IEnumerable<T> entities) => DbSet.AddRange(entities);

    public void Remove(T entity) => DbSet.Remove(entity);

    public void RemoveRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AnyAsync(predicate, ct).ConfigureAwait(false);
}

using System.Linq.Expressions;
using Greenlens.Domain.Common;

namespace Greenlens.Application.Common.Interfaces.Persistence;

/// <summary>
/// Base repository interface. Every entity has its own IXxxRepository inheriting this.
/// Handlers inject specific repos, NEVER IGenericRepository directly.
/// </summary>
public interface IGenericRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();
    IQueryable<T> QueryAsNoTracking();
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}

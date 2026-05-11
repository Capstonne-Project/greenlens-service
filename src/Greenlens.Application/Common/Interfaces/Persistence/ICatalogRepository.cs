using System.Linq.Expressions;

namespace Greenlens.Application.Common.Interfaces.Persistence;

/// <summary>
/// Read-oriented repository for seeded catalog entities keyed by string/int (not <see cref="Greenlens.Domain.Common.BaseEntity"/>).
/// Aggregates use <see cref="IGenericRepository{T}"/>.
/// </summary>
public interface ICatalogRepository<T> where T : class
{
    IQueryable<T> Query();
    IQueryable<T> QueryAsNoTracking();
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}

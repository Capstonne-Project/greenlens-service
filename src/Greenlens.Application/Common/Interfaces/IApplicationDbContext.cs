using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Application-layer abstraction over EF Core DbContext.
/// Infrastructure provides the concrete implementation.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}


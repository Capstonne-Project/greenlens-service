namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Abstracts database transaction management for the Application layer.
/// Infrastructure implements this via EF Core DbContext.Database.
/// </summary>
public interface ITransactionManager
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IPollutionCategoryRepository : IGenericRepository<PollutionCategory>
{
    /// <summary>Check code uniqueness including soft-deleted rows (DB unique index is not filtered).</summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludeCategoryId = null, CancellationToken ct = default);

    Task<bool> ExistsActiveAsync(Guid id, CancellationToken ct = default);

    Task<PollutionCategory?> GetActiveByCodeAsync(string code, CancellationToken ct = default);
}

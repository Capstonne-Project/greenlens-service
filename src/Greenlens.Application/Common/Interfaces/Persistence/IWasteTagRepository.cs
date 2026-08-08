using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IWasteTagRepository : IGenericRepository<WasteTag>
{
    /// <summary>Check code uniqueness including soft-deleted rows (DB unique index is not filtered).</summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludeTagId = null, CancellationToken ct = default);

    Task<List<WasteTag>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<WasteTag>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
}

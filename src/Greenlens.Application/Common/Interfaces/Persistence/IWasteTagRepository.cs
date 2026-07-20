using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IWasteTagRepository : IGenericRepository<WasteTag>
{
    Task<List<WasteTag>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<WasteTag>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
}

using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IPollutionCategoryRepository : IGenericRepository<PollutionCategory>
{
    Task<bool> ExistsActiveAsync(Guid id, CancellationToken ct = default);
}

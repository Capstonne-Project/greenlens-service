using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IBadgeRepository : IGenericRepository<Badge>
{
    Task<List<Badge>> GetAllActiveAsync(CancellationToken ct = default);
}

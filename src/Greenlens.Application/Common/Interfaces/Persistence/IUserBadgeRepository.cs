using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IUserBadgeRepository : IGenericRepository<UserBadge>
{
    Task<List<UserBadge>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasBadgeAsync(Guid userId, Guid badgeId, CancellationToken ct = default);
}

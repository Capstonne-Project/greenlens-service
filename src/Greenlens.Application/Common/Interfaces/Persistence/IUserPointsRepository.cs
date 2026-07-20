using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IUserPointsRepository : IGenericRepository<UserPoints>
{
    /// <summary>Get UserPoints by UserId, including transactions. Creates if not exists.</summary>
    Task<UserPoints> GetOrCreateByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Get UserPoints by UserId without tracking (for reads).</summary>
    Task<UserPoints?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}

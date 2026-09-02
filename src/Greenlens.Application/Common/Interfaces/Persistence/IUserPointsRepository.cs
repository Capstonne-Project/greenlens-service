using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IUserPointsRepository : IGenericRepository<UserPoints>
{
    /// <summary>Get UserPoints by UserId, including transactions. Creates if not exists.</summary>
    Task<UserPoints> GetOrCreateByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Get UserPoints by UserId without tracking (for reads).</summary>
    Task<UserPoints?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Idempotency guard at DB level — includes soft-deleted rows so partial unique index
    /// (which may still block re-insert) does not cause silent SaveChanges failures.
    /// </summary>
    Task<bool> HasTransactionForReportAsync(
        Guid userId, Guid reportId, PointReason reason, CancellationToken ct = default);
}

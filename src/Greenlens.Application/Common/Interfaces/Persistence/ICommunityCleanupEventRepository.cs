using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface ICommunityCleanupEventRepository : IGenericRepository<CommunityCleanupEvent>
{
    /// <summary>BR-CMU-003: the single active (not Completed/Cancelled) event for a report, if any.</summary>
    Task<CommunityCleanupEvent?> GetActiveByReportIdAsync(Guid reportId, CancellationToken ct = default);
}

using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface ICommunityCleanupParticipantRepository : IGenericRepository<CommunityCleanupParticipant>
{
    Task<CommunityCleanupParticipant?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken ct = default);
    Task<List<CommunityCleanupParticipant>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task<int> CountActiveByEventIdAsync(Guid eventId, CancellationToken ct = default);
}

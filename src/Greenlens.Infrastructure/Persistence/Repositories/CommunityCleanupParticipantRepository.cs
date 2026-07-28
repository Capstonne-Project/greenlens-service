using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class CommunityCleanupParticipantRepository(ApplicationDbContext db)
    : GenericRepository<CommunityCleanupParticipant>(db), ICommunityCleanupParticipantRepository
{
    public Task<CommunityCleanupParticipant?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken ct = default)
        => Query().FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId, ct);

    public Task<List<CommunityCleanupParticipant>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
        => Query()
            .Where(p => p.EventId == eventId)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(ct);

    public Task<int> CountActiveByEventIdAsync(Guid eventId, CancellationToken ct = default)
        => QueryAsNoTracking()
            .CountAsync(p => p.EventId == eventId
                && p.Status != CommunityCleanupParticipantStatus.Withdrawn
                && p.Status != CommunityCleanupParticipantStatus.NoShow, ct);
}

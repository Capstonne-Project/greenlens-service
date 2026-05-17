using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class TeamMemberRepository(ApplicationDbContext db)
    : GenericRepository<TeamMember>(db), ITeamMemberRepository
{
    public Task<bool> IsUserInTeamAsync(Guid teamId, Guid userId, CancellationToken ct = default)
        => QueryAsNoTracking()
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
}

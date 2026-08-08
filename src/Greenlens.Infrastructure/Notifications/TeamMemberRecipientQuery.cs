using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Notifications;

internal sealed class TeamMemberRecipientQuery(ApplicationDbContext db) : ITeamMemberRecipientQuery
{
    public async Task<IReadOnlyList<Guid>> GetActiveMemberUserIdsAsync(
        Guid teamId,
        CancellationToken ct = default)
        => await db.TeamMembers
            .AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .Join(
                db.Users.AsNoTracking().Where(u => !u.IsBanned),
                m => m.UserId,
                u => u.Id,
                (m, u) => u.Id)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

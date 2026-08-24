using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class TeamWasteTagRepository(ApplicationDbContext context) : ITeamWasteTagRepository
{
    private readonly DbSet<TeamWasteTag> _dbSet = context.Set<TeamWasteTag>();

    public async Task<List<TeamWasteTag>> GetByTeamIdAsync(Guid teamId, CancellationToken ct = default) =>
        await _dbSet.Where(tw => tw.TeamId == teamId).ToListAsync(ct).ConfigureAwait(false);

    public void AddRange(IEnumerable<TeamWasteTag> entities) => _dbSet.AddRange(entities);

    public void RemoveRange(IEnumerable<TeamWasteTag> entities) => _dbSet.RemoveRange(entities);
}

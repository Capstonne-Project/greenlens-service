using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface ITeamWasteTagRepository
{
    Task<List<TeamWasteTag>> GetByTeamIdAsync(Guid teamId, CancellationToken ct = default);
    void AddRange(IEnumerable<TeamWasteTag> entities);
    void RemoveRange(IEnumerable<TeamWasteTag> entities);
}

using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface ITeamMemberRepository : IGenericRepository<TeamMember>
{
    Task<bool> IsUserInTeamAsync(Guid teamId, Guid userId, CancellationToken ct = default);
    Task<TeamMember?> GetLeaderByUserIdAsync(Guid userId, CancellationToken ct = default);
}

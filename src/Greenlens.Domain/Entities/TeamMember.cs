using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Join table: User ↔ EnvironmentalTeam membership.
/// One user can belong to one team. IsLeader marks the Team Leader
/// (required for Inspection Team to issue Penalty Decisions per BR-INS-012).
/// </summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed class TeamMember : BaseEntity
{
    private TeamMember() { } // EF Core constructor

    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsLeader { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // ── Navigation ──
    public EnvironmentalTeam? Team { get; private set; }
    public User? User { get; private set; }

    public static TeamMember Create(Guid teamId, Guid userId, bool isLeader = false)
    {
        return new TeamMember
        {
            TeamId = teamId,
            UserId = userId,
            IsLeader = isLeader,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void PromoteToLeader() => IsLeader = true;
    public void DemoteFromLeader() => IsLeader = false;
}

namespace Greenlens.Application.Common.Interfaces;

/// <summary>Resolves cleanup/inspection team members for task notifications (BR-CLN-001).</summary>
public interface ITeamMemberRecipientQuery
{
    Task<IReadOnlyList<Guid>> GetActiveMemberUserIdsAsync(Guid teamId, CancellationToken ct = default);
}

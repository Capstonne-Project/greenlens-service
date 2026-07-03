using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.Inspection;

/// <summary>BR-INS-012: Only Inspection Team Leader assigned to the inspection may act.</summary>
internal static class InspectionTeamAuthorization
{
    public static async Task<Error?> ValidateTeamLeaderAsync(
        InspectionReport inspection,
        ITeamMemberRepository teamMembers,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
            return Errors.Inspections.NotTeamLeader;

        if (inspection.AssignedTeamId != leader.TeamId)
            return Errors.Inspections.NotAssignedToYourTeam;

        return null;
    }
}

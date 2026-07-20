using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.TransferTeamMember;

/// <summary>
/// Atomically transfers a team member from one team to another within the LEO's office.
/// Validates: both teams belong to LEO's office, role-TeamType compatibility, user is in old team.
/// </summary>
public sealed class TransferTeamMemberCommandHandler(
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<TransferTeamMemberCommandHandler> logger) : IRequestHandler<TransferTeamMemberCommand, Result<TransferTeamMemberResponse>>
{
    public async Task<Result<TransferTeamMemberResponse>> Handle(
        TransferTeamMemberCommand request,
        CancellationToken ct)
    {
        // ── 1. Verify LEO has an office ──
        var leo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leo is null)
            return Errors.Users.UserNotFound;

        if (!leo.LocalOfficeId.HasValue)
            return Errors.Organization.OfficerNoOffice;

        var leoOfficeId = leo.LocalOfficeId.Value;

        // ── 2. Cannot transfer to the same team ──
        if (request.CurrentTeamId == request.NewTeamId)
            return Errors.Organization.TransferSameTeam;

        // ── 3. Load both teams ──
        var oldTeam = await teams.GetByIdAsync(request.CurrentTeamId, ct).ConfigureAwait(false);
        if (oldTeam is null)
            return Errors.Organization.TeamNotFound;

        var newTeam = await teams.GetByIdAsync(request.NewTeamId, ct).ConfigureAwait(false);
        if (newTeam is null)
            return Errors.Organization.TeamNotFound;

        // ── 4. Both teams must belong to LEO's office ──
        if (oldTeam.LocalOfficeId != leoOfficeId || newTeam.LocalOfficeId != leoOfficeId)
            return Errors.Organization.TeamNotInOffice;

        // ── 5. Load the user ──
        var user = await users.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        // ── 6. Validate role-TeamType compatibility with NEW team ──
        var validRole = (user.Role, newTeam.TeamType) switch
        {
            (UserRole.Cleaner, TeamType.Cleanup) => true,
            (UserRole.Inspector, TeamType.Inspection) => true,
            _ => false
        };

        if (!validRole)
            return Errors.Organization.InvalidRoleForTeamMember;

        // ── 7. Find existing membership in old team ──
        var existingMember = await teamMembers.QueryAsNoTracking()
            .FirstOrDefaultAsync(m => m.TeamId == request.CurrentTeamId && m.UserId == request.UserId, ct)
            .ConfigureAwait(false);

        if (existingMember is null)
            return Errors.Organization.MemberNotInTeam;

        // ── 8. Check not already in new team ──
        var alreadyInNewTeam = await teamMembers
            .IsUserInTeamAsync(request.NewTeamId, request.UserId, ct)
            .ConfigureAwait(false);

        if (alreadyInNewTeam)
            return Errors.Organization.MemberAlreadyInTeam;

        // ── 9. Atomic: remove from old team + add to new team ──
        var tracked = await teamMembers.GetByIdAsync(existingMember.Id, ct).ConfigureAwait(false);
        if (tracked is not null)
            teamMembers.Remove(tracked);

        var newMember = TeamMember.Create(request.NewTeamId, request.UserId, request.IsLeader);
        teamMembers.Add(newMember);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "LEO {LeoId} transferred user {UserId} from team {OldTeamId} to team {NewTeamId}",
            currentUser.UserId, request.UserId, request.CurrentTeamId, request.NewTeamId);

        return new TransferTeamMemberResponse(
            request.UserId,
            request.CurrentTeamId,
            request.NewTeamId,
            newMember.Id,
            request.IsLeader);
    }
}

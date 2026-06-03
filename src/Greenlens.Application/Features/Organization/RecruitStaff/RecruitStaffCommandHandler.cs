using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.RecruitStaff;

/// <summary>
/// Recruits a Citizen user into the LEO's LocalOffice by:
/// 1. Changing their role to Cleaner/Inspector.
/// 2. Setting their LocalOfficeId.
/// 3. Optionally creating a TeamMember record.
/// All within a single transaction.
/// </summary>
public sealed class RecruitStaffCommandHandler(
    IUserRepository users,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RecruitStaffCommandHandler> logger) : IRequestHandler<RecruitStaffCommand, Result<RecruitStaffResponse>>
{
    public async Task<Result<RecruitStaffResponse>> Handle(
        RecruitStaffCommand request,
        CancellationToken ct)
    {
        // ── 1. Verify current user is LEO with an assigned office ──
        var leo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leo is null)
            return Errors.Users.UserNotFound;

        if (!leo.LocalOfficeId.HasValue)
            return Errors.Organization.OfficerNoOffice;

        var leoOfficeId = leo.LocalOfficeId.Value;

        // ── 2. Find target user by email ──
        var targetUser = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (targetUser is null)
            return Errors.Users.UserNotFound;

        // ── 3. Validate current role = Citizen ──
        if (targetUser.Role != UserRole.Citizen)
            return Errors.Organization.InvalidRoleForRecruit;

        // ── 4. Validate target role is Cleaner or Inspector ──
        if (request.TargetRole is not (UserRole.Cleaner or UserRole.Inspector))
            return Errors.Organization.InvalidRoleForTeamMember;

        // ── 5. Check user not already assigned to another office ──
        if (targetUser.LocalOfficeId.HasValue)
            return Errors.Organization.UserAlreadyInOffice;

        // ── 6. Change role + assign to office ──
        targetUser.ChangeRole(request.TargetRole);
        targetUser.AssignToLocalOffice(leoOfficeId);

        // ── 7. Optionally add to team ──
        Guid? teamMemberId = null;
        Guid? assignedTeamId = null;

        if (request.TeamId.HasValue)
        {
            var team = await teams.GetByIdAsync(request.TeamId.Value, ct).ConfigureAwait(false);

            if (team is null)
                return Errors.Organization.TeamNotFound;

            // Team must belong to the LEO's office
            if (team.LocalOfficeId != leoOfficeId)
                return Errors.Organization.TeamNotInOffice;

            // Role-TeamType compatibility: Cleaner→Cleanup, Inspector→Inspection
            var roleMatchesTeam = (request.TargetRole, team.TeamType) switch
            {
                (UserRole.Cleaner, TeamType.Cleanup) => true,
                (UserRole.Inspector, TeamType.Inspection) => true,
                _ => false
            };

            if (!roleMatchesTeam)
                return Errors.Organization.InvalidRoleForTeamMember;

            // Check not already in any team
            var alreadyInTeam = await teamMembers
                .ExistsAsync(tm => tm.UserId == targetUser.Id, ct)
                .ConfigureAwait(false);

            if (alreadyInTeam)
                return Errors.Organization.UserAlreadyInTeam;

            var member = TeamMember.Create(team.Id, targetUser.Id, request.IsLeader);
            teamMembers.Add(member);
            teamMemberId = member.Id;
            assignedTeamId = team.Id;
        }

        // ── 8. Persist ──
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "LEO {LeoId} recruited user {UserId} ({Email}) as {Role} to office {OfficeId}, team={TeamId}",
            currentUser.UserId, targetUser.Id, targetUser.Email, request.TargetRole, leoOfficeId, assignedTeamId);

        return new RecruitStaffResponse(
            targetUser.Id,
            targetUser.Email,
            targetUser.FullName,
            request.TargetRole,
            leoOfficeId,
            assignedTeamId,
            teamMemberId);
    }
}

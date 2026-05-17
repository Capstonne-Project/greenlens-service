using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.AddTeamMember;

/// <summary>
/// Adds a user to an Environmental Team. Validates role compatibility.
/// </summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed class AddTeamMemberCommandHandler(
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    IUserRepository users,
    IUnitOfWork uow) : IRequestHandler<AddTeamMemberCommand, Result<AddTeamMemberResponse>>
{
    public async Task<Result<AddTeamMemberResponse>> Handle(
        AddTeamMemberCommand request,
        CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(request.TeamId, cancellationToken)
            .ConfigureAwait(false);

        if (team is null)
            return Errors.Organization.TeamNotFound;

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Users.UserNotFound;

        // Validate role compatibility: Cleanup team → Cleanup role, Inspection team → Inspector role
        var validRole = team.TeamType switch
        {
            TeamType.Cleanup => user.Role == UserRole.Cleanup,
            TeamType.Inspection => user.Role == UserRole.Inspector,
            _ => false
        };

        if (!validRole)
            return Errors.Organization.InvalidRoleForTeamMember;

        // Check if already a member
        var alreadyMember = await teamMembers.IsUserInTeamAsync(request.TeamId, request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyMember)
            return Errors.Organization.MemberAlreadyInTeam;

        var member = TeamMember.Create(request.TeamId, request.UserId, request.IsLeader);
        teamMembers.Add(member);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AddTeamMemberResponse(member.Id, member.TeamId, member.UserId, member.IsLeader);
    }
}

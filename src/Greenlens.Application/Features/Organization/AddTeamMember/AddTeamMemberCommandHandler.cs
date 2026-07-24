using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.AddTeamMember;

/// <summary>
/// Adds a user to an Environmental Team. Validates role compatibility.
/// </summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed class AddTeamMemberCommandHandler(
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    IUserRepository users,
    IUnitOfWork uow,
    ILogger<AddTeamMemberCommandHandler> logger) : IRequestHandler<AddTeamMemberCommand, Result<AddTeamMemberResponse>>
{
    public async Task<Result<AddTeamMemberResponse>> Handle(
        AddTeamMemberCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding team member {UserId} to team {TeamId}", request.UserId, request.TeamId);

        var team = await teams.GetByIdAsync(request.TeamId, cancellationToken)
            .ConfigureAwait(false);

        if (team is null)
        {
            logger.LogWarning("Team {TeamId} not found", request.TeamId);
            return Errors.Organization.TeamNotFound;
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return Errors.Users.UserNotFound;
        }

        // Validate role compatibility: Cleanup team → Cleaner role, Inspection team → Inspector role
        var validRole = team.TeamType switch
        {
            TeamType.Cleanup => user.Role == UserRole.Cleaner,
            TeamType.Inspection => user.Role == UserRole.Inspector,
            _ => false
        };

        if (!validRole)
        {
            logger.LogWarning("User {UserId} has invalid role for team {TeamId}", request.UserId, request.TeamId);
            return Errors.Organization.InvalidRoleForTeamMember;
        }

        // Check if already a member
        var alreadyMember = await teamMembers.IsUserInTeamAsync(request.TeamId, request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyMember)
        {
            logger.LogWarning("User {UserId} is already a member of team {TeamId}", request.UserId, request.TeamId);
            return Errors.Organization.MemberAlreadyInTeam;
        }

        var member = TeamMember.Create(request.TeamId, request.UserId, request.IsLeader);
        teamMembers.Add(member);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} added to team {TeamId} (leader={IsLeader})",
            request.UserId, request.TeamId, request.IsLeader);

        return new AddTeamMemberResponse(member.Id, member.TeamId, member.UserId, member.IsLeader);
    }
}

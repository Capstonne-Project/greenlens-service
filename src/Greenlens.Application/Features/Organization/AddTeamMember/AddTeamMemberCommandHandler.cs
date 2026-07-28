using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.AddTeamMember;

/// <summary>
/// Adds a user to a community Environmental Team. Validates role compatibility and LEO office scope.
/// </summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed class AddTeamMemberCommandHandler(
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AddTeamMemberCommandHandler> logger) : IRequestHandler<AddTeamMemberCommand, Result<AddTeamMemberResponse>>
{
    public async Task<Result<AddTeamMemberResponse>> Handle(
        AddTeamMemberCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding team member {UserId} to team {TeamId}", request.UserId, request.TeamId);

        var (actor, actorError) = await TeamAccessAuthorization
            .ResolveActorAsync(users, currentUser, cancellationToken)
            .ConfigureAwait(false);

        if (actorError is not null)
        {
            logger.LogWarning("Actor error: {ActorError}", actorError.Code);
            return actorError;
        }

        var team = await teams.GetByIdAsync(request.TeamId, cancellationToken)
            .ConfigureAwait(false);

        if (team is null)
        {
            logger.LogWarning("Team {TeamId} not found", request.TeamId);
            return Errors.Organization.TeamNotFound;
        }

        var scopeError = TeamAccessAuthorization.ValidateLeoManageCommunityTeam(team, actor!);
        if (scopeError is not null)
        {
            logger.LogWarning(
                "User {UserId} cannot manage team {TeamId}: {ErrorCode}",
                currentUser.UserId, request.TeamId, scopeError.Code);
            return scopeError;
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return Errors.Users.UserNotFound;
        }

        if (!user.LocalOfficeId.HasValue || user.LocalOfficeId != team.LocalOfficeId)
        {
            logger.LogWarning(
                "User {UserId} is not staff of office {OfficeId}",
                request.UserId, team.LocalOfficeId);
            return Errors.Organization.UserNotInYourOffice;
        }

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

        var existingMembership = await teamMembers.GetByUserIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (existingMembership is not null)
        {
            if (existingMembership.TeamId == request.TeamId)
            {
                logger.LogWarning("User {UserId} is already a member of team {TeamId}", request.UserId, request.TeamId);
                return Errors.Organization.MemberAlreadyInTeam;
            }

            logger.LogWarning(
                "User {UserId} is already a member of team {ExistingTeamId}",
                request.UserId, existingMembership.TeamId);
            return Errors.Organization.UserAlreadyInTeam;
        }

        var member = TeamMember.Create(request.TeamId, request.UserId, request.IsLeader);
        teamMembers.Add(member);

        try
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
                return mapped;

            throw;
        }

        logger.LogInformation("User {UserId} added to team {TeamId} (leader={IsLeader})",
            request.UserId, request.TeamId, request.IsLeader);

        return new AddTeamMemberResponse(member.Id, member.TeamId, member.UserId, member.IsLeader);
    }
}

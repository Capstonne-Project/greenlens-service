using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.AcceptInvitation;

/// <summary>
/// Citizen accepts a staff invitation — role changes, assigned to office + optional team.
/// </summary>
/// <remarks>Implements: BR-ORG-021 (accept invitation, single-use, role change).</remarks>
public sealed class AcceptInvitationCommandHandler(
    IStaffInvitationRepository invitations,
    IUserRepository users,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AcceptInvitationCommandHandler> logger)
    : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>
{
    public async Task<Result<AcceptInvitationResponse>> Handle(
        AcceptInvitationCommand request,
        CancellationToken ct)
    {
        var invitation = await invitations.GetByIdAsync(request.InvitationId, ct)
            .ConfigureAwait(false);

        if (invitation is null)
            return Errors.Organization.InvitationNotFound;

        // Verify the current user is the invited person
        if (invitation.InvitedUserId != currentUser.UserId)
            return Errors.Auth.Forbidden;

        // Domain handles expiry + status validation
        var acceptResult = invitation.Accept();
        if (!acceptResult.IsSuccess)
            return Result<AcceptInvitationResponse>.Failure(acceptResult.Error!);

        // Change role + assign to office
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        user.ChangeRole(invitation.TargetRole);
        user.AssignToLocalOffice(invitation.LocalOfficeId);

        // Optionally add to team
        if (invitation.TeamId.HasValue)
        {
            var member = TeamMember.Create(invitation.TeamId.Value, user.Id, isLeader: false);
            teamMembers.Add(member);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "User {UserId} accepted invitation {InvitationId}, now {Role} in office {OfficeId}",
            user.Id, invitation.Id, invitation.TargetRole, invitation.LocalOfficeId);

        return new AcceptInvitationResponse(
            user.Id,
            invitation.TargetRole,
            invitation.LocalOfficeId,
            invitation.TeamId);
    }
}

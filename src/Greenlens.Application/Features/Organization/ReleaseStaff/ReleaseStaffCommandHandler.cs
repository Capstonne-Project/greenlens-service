using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.ReleaseStaff;

/// <summary>
/// Reverts a staff member (Cleaner/Inspector) back to Citizen role.
/// Removes from office assignment and all team memberships.
/// </summary>
public sealed class ReleaseStaffCommandHandler(
    IUserRepository users,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ReleaseStaffCommandHandler> logger)
    : IRequestHandler<ReleaseStaffCommand, Result>
{
    public async Task<Result> Handle(ReleaseStaffCommand request, CancellationToken ct)
    {
        var targetUser = await users.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (targetUser is null)
            return Errors.Users.UserNotFound;

        // Cannot release a Citizen
        if (targetUser.Role == UserRole.Citizen)
            return Errors.Organization.CannotReleaseCitizen;

        // LEO can only release staff in their own office
        var leo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leo is null)
            return Errors.Users.UserNotFound;

        if (leo.LocalOfficeId.HasValue
            && targetUser.LocalOfficeId.HasValue
            && leo.LocalOfficeId != targetUser.LocalOfficeId)
        {
            return Errors.Organization.UserNotInYourOffice;
        }

        // Remove all team memberships
        var memberships = await teamMembers.Query()
            .Where(tm => tm.UserId == request.UserId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var m in memberships)
        {
            teamMembers.Remove(m);
        }

        // Revert role to Citizen + clear office
        targetUser.ChangeRole(UserRole.Citizen);
        targetUser.ClearOfficeAssignment();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "User {UserId} released back to Citizen by {ReleasedBy}, removed from {TeamCount} teams",
            request.UserId, currentUser.UserId, memberships.Count);

        return Result.Success();
    }
}

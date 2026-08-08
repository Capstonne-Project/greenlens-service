using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetMyInvitations;

/// <summary>BR-ORG-021: Returns all invitations for the current user.</summary>
public sealed class GetMyInvitationsQueryHandler(
    IStaffInvitationRepository invitations,
    ICurrentUser currentUser,
    ILogger<GetMyInvitationsQueryHandler> logger)
    : IRequestHandler<GetMyInvitationsQuery, Result<List<InvitationDto>>>
{
    public async Task<Result<List<InvitationDto>>> Handle(
        GetMyInvitationsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting my invitations for user {UserId}", currentUser.UserId);

        var result = await invitations.QueryAsNoTracking()
            .Where(i => i.InvitedUserId == currentUser.UserId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationDto(
                i.Id,
                i.InvitedByUserId,
                i.InvitedByUser != null ? i.InvitedByUser.FullName : "",
                i.TargetRole,
                i.LocalOffice != null ? i.LocalOffice.Name : "",
                i.Team != null ? i.Team.Name : null,
                // Mark as Expired if past due
                i.Status == InvitationStatus.Pending && i.ExpiresAt <= DateTime.UtcNow
                    ? InvitationStatus.Expired
                    : i.Status,
                i.ExpiresAt,
                i.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return result;
    }
}

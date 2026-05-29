using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.RemoveTeamMember;

public sealed class RemoveTeamMemberCommandHandler(
    ITeamMemberRepository members,
    IUnitOfWork uow,
    ILogger<RemoveTeamMemberCommandHandler> logger) : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken ct)
    {
        // Find member by team + user composite key
        var member = await members.QueryAsNoTracking()
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId && m.UserId == request.UserId, ct)
            .ConfigureAwait(false);

        if (member is null)
            return Errors.Organization.MemberNotFound;

        // Re-fetch tracked
        var tracked = await members.GetByIdAsync(member.Id, ct).ConfigureAwait(false);
        if (tracked is not null)
        {
            members.Remove(tracked);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation("User {UserId} removed from team {TeamId}",
                request.UserId, request.TeamId);
        }

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.RemoveTeamMember;

public sealed class RemoveTeamMemberCommandHandler(
    ITeamMemberRepository members,
    IUnitOfWork uow) : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken ct)
    {
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
        }

        return Result.Success();
    }
}

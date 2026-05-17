using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateTeam;

public sealed class UpdateTeamCommandHandler(
    IEnvironmentalTeamRepository teams,
    IUnitOfWork uow) : IRequestHandler<UpdateTeamCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamCommand request, CancellationToken ct)
    {
        var team = await teams.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (team is null)
            return Errors.Organization.TeamNotFound;

        team.Update(request.Name);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

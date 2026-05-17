using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateTeam;

/// <summary>
/// Creates an Environmental Team under a LocalOffice.
/// </summary>
/// <remarks>Implements: BR-ORG-003, BR-ADM-011.</remarks>
public sealed class CreateTeamCommandHandler(
    IEnvironmentalTeamRepository teams,
    ILocalOfficeRepository localOffices,
    IUnitOfWork uow) : IRequestHandler<CreateTeamCommand, Result<CreateTeamResponse>>
{
    public async Task<Result<CreateTeamResponse>> Handle(
        CreateTeamCommand request,
        CancellationToken cancellationToken)
    {
        var office = await localOffices.GetByIdAsync(request.LocalOfficeId, cancellationToken)
            .ConfigureAwait(false);

        if (office is null)
            return Errors.Organization.LocalOfficeNotFound;

        var team = EnvironmentalTeam.Create(request.Name, request.LocalOfficeId, request.TeamType);
        teams.Add(team);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateTeamResponse(team.Id, team.Name, team.LocalOfficeId, team.TeamType.ToString());
    }
}

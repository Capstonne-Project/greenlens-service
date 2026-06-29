using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateTeam;

/// <summary>
/// LEO tạo team cộng đồng. LocalOfficeId auto-resolve từ ICurrentUser.
/// </summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed class CreateTeamCommandHandler(
    ICurrentUser currentUser,
    IEnvironmentalTeamRepository teams,
    ILocalOfficeRepository localOffices,
    IUnitOfWork uow,
    ILogger<CreateTeamCommandHandler> logger) : IRequestHandler<CreateTeamCommand, Result<CreateTeamResponse>>
{
    public async Task<Result<CreateTeamResponse>> Handle(
        CreateTeamCommand request,
        CancellationToken cancellationToken)
    {
        // ── Resolve LEO's office from token ──
        var office = await localOffices.QueryAsNoTracking()
            .FirstOrDefaultAsync(o => o.OfficerId == currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (office is null)
            return Errors.Organization.OfficeNotFound;

        var team = EnvironmentalTeam.Create(request.Name, office.Id, request.TeamType);
        teams.Add(team);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Team {TeamId} ({TeamType}) created under office {OfficeId} by LEO {UserId}",
            team.Id, team.TeamType, office.Id, currentUser.UserId);

        return new CreateTeamResponse(team.Id, team.Name, office.Id, team.TeamType.ToString());
    }
}

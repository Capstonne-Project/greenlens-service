using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.SoftDeleteCompanyTeam;

public sealed class SoftDeleteCompanyTeamCommandHandler(
    IEnvironmentalTeamRepository teams,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<SoftDeleteCompanyTeamCommandHandler> logger) : IRequestHandler<SoftDeleteCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(SoftDeleteCompanyTeamCommand request, CancellationToken ct)
    {
        var team = await teams.GetByIdAsync(request.TeamId, ct).ConfigureAwait(false);
        if (team is null)
            return Errors.Organization.TeamNotFound;

        team.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Company Team {TeamId} soft-deleted by Admin {UserId}", request.TeamId, currentUser.UserId);

        return Result.Success();
    }
}

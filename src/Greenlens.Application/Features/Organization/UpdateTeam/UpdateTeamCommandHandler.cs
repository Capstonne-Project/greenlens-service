using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.UpdateTeam;

/// <remarks>Implements: BR-ORG-003, BR-CLN-005.</remarks>
public sealed class UpdateTeamCommandHandler(
    IEnvironmentalTeamRepository teams,
    TeamWasteTagService wasteTagService,
    IUnitOfWork uow,
    ILogger<UpdateTeamCommandHandler> logger) : IRequestHandler<UpdateTeamCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating team for team {TeamId}", request.Id);

        var team = await teams.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Team not found for ID {Id}", request.Id);
            return Errors.Organization.TeamNotFound;
        }

        if (request.WasteTagIds is not null)
        {
            var tagResult = await wasteTagService
                .ReplaceTeamTagsAsync(team, request.WasteTagIds, ct)
                .ConfigureAwait(false);
            if (!tagResult.IsSuccess)
                return tagResult;
        }

        team.Update(request.Name);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Team {TeamId} updated", request.Id);

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.UpdateCompanyTeam;

/// <remarks>Implements: BR-CMP-004, BR-CLN-005.</remarks>
public sealed class UpdateCompanyTeamCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    TeamWasteTagService wasteTagService,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<UpdateCompanyTeamCommandHandler> logger) : IRequestHandler<UpdateCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(UpdateCompanyTeamCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating company team for team {TeamId}", request.TeamId);

        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null)
        {
            logger.LogWarning("Company staff not found for user ID {UserId}", currentUser.UserId);
            return Errors.Organization.NotCompanyManager;
        }

        var team = await teams.GetByIdAsync(request.TeamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Team not found for ID {TeamId}", request.TeamId);
            return Errors.Organization.TeamNotFound;
        }

        if (team.CompanyId != staff.CompanyId)
        {
            logger.LogWarning("Team {TeamId} is not in the company {CompanyId}", request.TeamId, staff.CompanyId);
            return Errors.Organization.TeamNotInCompany;
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

        logger.LogInformation("Company team {TeamId} updated by CM {UserId}", request.TeamId, currentUser.UserId);

        return Result.Success();
    }
}

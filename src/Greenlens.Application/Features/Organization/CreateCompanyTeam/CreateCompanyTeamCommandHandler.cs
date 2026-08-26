using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateCompanyTeam;

/// <remarks>Implements: BR-CMP-004, BR-CLN-005.</remarks>
public sealed class CreateCompanyTeamCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    TeamWasteTagService wasteTagService,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<CreateCompanyTeamCommandHandler> logger) : IRequestHandler<CreateCompanyTeamCommand, Result<CreateCompanyTeamResponse>>
{
    public async Task<Result<CreateCompanyTeamResponse>> Handle(
        CreateCompanyTeamCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating company team {Name} for user {UserId}", request.Name, currentUser.UserId);

        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (staff is null)
        {
            logger.LogWarning("Company manager not found for user ID {UserId}", currentUser.UserId);
            return Errors.Organization.NotCompanyManager;
        }

        var companyId = staff.CompanyId;
        var team = EnvironmentalTeam.CreateCompanyTeam(request.Name, TeamType.Cleanup, companyId);
        teams.Add(team);

        var tagResult = await wasteTagService
            .ReplaceTeamTagsAsync(team, request.WasteTagIds, cancellationToken)
            .ConfigureAwait(false);
        if (!tagResult.IsSuccess)
            return tagResult.Error!;

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var loaded = await teams.QueryAsNoTracking()
            .Include(t => t.WasteTags).ThenInclude(tw => tw.WasteTag)
            .FirstAsync(t => t.Id == team.Id, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Company team {TeamId} created by CM {UserId} for company {CompanyId}",
            team.Id, currentUser.UserId, companyId);

        return new CreateCompanyTeamResponse(
            team.Id,
            team.Name,
            companyId,
            team.TeamType.ToString(),
            TeamWasteTagService.MapTags(loaded));
    }
}

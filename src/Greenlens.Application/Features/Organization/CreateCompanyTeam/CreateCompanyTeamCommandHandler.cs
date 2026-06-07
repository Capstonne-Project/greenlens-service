using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateCompanyTeam;

/// <summary>
/// CompanyManager creates a CleanupTeam under their company.
/// No LocalOfficeId — company teams go wherever the dispatched task is.
/// Only Cleanup teams allowed — InspectionTeam is ward-level (LEO-managed).
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class CreateCompanyTeamCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<CreateCompanyTeamCommandHandler> logger) : IRequestHandler<CreateCompanyTeamCommand, Result<CreateCompanyTeamResponse>>
{
    public async Task<Result<CreateCompanyTeamResponse>> Handle(
        CreateCompanyTeamCommand request,
        CancellationToken cancellationToken)
    {
        // Resolve CompanyId from current user
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (staff is null)
            return Errors.Organization.NotCompanyManager;

        var companyId = staff.CompanyId;

        // Company teams can only be Cleanup (InspectionTeam is ward-level)
        var team = EnvironmentalTeam.CreateCompanyTeam(
            request.Name, TeamType.Cleanup, companyId);

        teams.Add(team);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Company team {TeamId} created by CM {UserId} for company {CompanyId}",
            team.Id, currentUser.UserId, companyId);

        return new CreateCompanyTeamResponse(
            team.Id, team.Name, companyId, team.TeamType.ToString());
    }
}

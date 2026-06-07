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
/// Only Cleanup teams allowed — InspectionTeam is ward-level (LEO-managed).
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class CreateCompanyTeamCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    ILocalOfficeRepository localOffices,
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

        // Verify office exists
        var office = await localOffices.GetByIdAsync(request.LocalOfficeId, cancellationToken)
            .ConfigureAwait(false);

        if (office is null)
            return Errors.Organization.LocalOfficeNotFound;

        // Company teams can only be Cleanup (InspectionTeam is ward-level)
        var team = EnvironmentalTeam.CreateCompanyTeam(
            request.Name, request.LocalOfficeId, TeamType.Cleanup, companyId);

        teams.Add(team);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Company team {TeamId} created by CM {UserId} for company {CompanyId} under office {OfficeId}",
            team.Id, currentUser.UserId, companyId, team.LocalOfficeId);

        return new CreateCompanyTeamResponse(
            team.Id, team.Name, team.LocalOfficeId, companyId, team.TeamType.ToString());
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.ToggleCompanyTeamStatus;

/// <summary>
/// CM toggles active status of a company team.
/// Validates team belongs to CM's company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class ToggleCompanyTeamStatusCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<ToggleCompanyTeamStatusCommandHandler> logger) : IRequestHandler<ToggleCompanyTeamStatusCommand, Result>
{
    public async Task<Result> Handle(ToggleCompanyTeamStatusCommand request, CancellationToken ct)
    {
        // Resolve CM's company
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null)
            return Errors.Organization.NotCompanyManager;

        var team = await teams.GetByIdAsync(request.TeamId, ct).ConfigureAwait(false);
        if (team is null)
            return Errors.Organization.TeamNotFound;

        if (team.CompanyId != staff.CompanyId)
            return Errors.Organization.TeamNotInCompany;

        if (request.IsActive)
            team.Activate();
        else
            team.Deactivate();
            
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Company team {TeamId} status changed to {IsActive} by CM {UserId}", request.TeamId, request.IsActive, currentUser.UserId);

        return Result.Success();
    }
}

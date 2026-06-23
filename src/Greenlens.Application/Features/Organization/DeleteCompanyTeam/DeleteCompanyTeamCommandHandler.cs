using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.DeleteCompanyTeam;

/// <summary>
/// CM deactivates a company team (soft-delete — team may have active assignments).
/// Validates team belongs to CM's company and is currently active.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class DeleteCompanyTeamCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteCompanyTeamCommandHandler> logger) : IRequestHandler<DeleteCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(DeleteCompanyTeamCommand request, CancellationToken ct)
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

        if (!team.IsActive)
            return Errors.Organization.TeamAlreadyDeactivated;

        team.Deactivate();
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Company team {TeamId} deactivated by CM {UserId}", request.TeamId, currentUser.UserId);

        return Result.Success();
    }
}

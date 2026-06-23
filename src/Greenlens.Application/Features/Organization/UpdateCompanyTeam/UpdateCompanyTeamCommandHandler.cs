using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.UpdateCompanyTeam;

/// <summary>
/// CM renames a company team. Validates team belongs to CM's company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class UpdateCompanyTeamCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<UpdateCompanyTeamCommandHandler> logger) : IRequestHandler<UpdateCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(UpdateCompanyTeamCommand request, CancellationToken ct)
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

        team.Update(request.Name);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Company team {TeamId} renamed by CM {UserId}", request.TeamId, currentUser.UserId);

        return Result.Success();
    }
}

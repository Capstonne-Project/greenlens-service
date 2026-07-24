using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.SoftDeleteCompanyTeam;

/// <summary>CM/Admin archives a company team when it has no in-flight assignments.</summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class SoftDeleteCompanyTeamCommandHandler(
    IEnvironmentalTeamRepository teams,
    ICompanyStaffRepository companyStaff,
    IReportAssignmentRepository assignments,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<SoftDeleteCompanyTeamCommandHandler> logger) : IRequestHandler<SoftDeleteCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(SoftDeleteCompanyTeamCommand request, CancellationToken ct)
    {
        logger.LogInformation("Soft deleting company team for team {TeamId}", request.TeamId);

        var team = await teams.GetByIdAsync(request.TeamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Team {TeamId} not found", request.TeamId);
            return Errors.Organization.TeamNotFound;
        }

        if (team.IsDeleted)
        {
            logger.LogWarning("Team {TeamId} already deleted", request.TeamId);
            return Errors.Organization.TeamAlreadyDeleted;
        }

        if (!team.IsCompanyTeam)
        {
            logger.LogWarning("Team {TeamId} is not in a company", request.TeamId);
            return Errors.Organization.TeamNotInCompany;
        }

        if (currentUser.Role == "CompanyManager")
        {
            var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
            if (staff is null)
            {
                logger.LogWarning("Company staff not found for user ID {UserId}", currentUser.UserId);
                return Errors.Organization.NotCompanyManager;
            }

            if (team.CompanyId != staff.CompanyId)
            {
                logger.LogWarning("Team {TeamId} is not in the company {CompanyId}", request.TeamId, staff.CompanyId);
                return Errors.Organization.TeamNotInCompany;
            }
        }

        var activeAssignments = await assignments
            .CountInProgressByTeamAsync(team.Id, ct)
            .ConfigureAwait(false);

        try
        {
            team.Archive(currentUser.UserId.ToString(), activeAssignments > 0);
        }
        catch (DomainException ex) when (ex.Message.Contains("active assignments", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Team {TeamId} has active assignments", request.TeamId);
            return Errors.Organization.TeamHasActiveAssignments;
        }
        catch (DomainException)
        {
            logger.LogWarning("Team {TeamId} already deleted", request.TeamId);
            return Errors.Organization.TeamAlreadyDeleted;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Company team {TeamId} archived by {UserId}",
            request.TeamId, currentUser.UserId);

        return Result.Success();
    }
}

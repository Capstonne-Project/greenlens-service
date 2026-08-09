using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.RemoveCompanyTeamMember;

/// <summary>
/// CM removes a member from a company team.
/// Validates: CM owns team, member exists in team.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class RemoveCompanyTeamMemberCommandHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository members,
    IReportAssignmentRepository assignments,
    IInspectionReportRepository inspections,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<RemoveCompanyTeamMemberCommandHandler> logger) : IRequestHandler<RemoveCompanyTeamMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveCompanyTeamMemberCommand request, CancellationToken ct)
    {
        logger.LogInformation("Removing company team member for user {UserId}", request.UserId);

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

        var member = await members.QueryAsNoTracking()
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId && m.UserId == request.UserId, ct)
            .ConfigureAwait(false);

        if (member is null)
        {
            logger.LogWarning("Member not found for team ID {TeamId} and user ID {UserId}", request.TeamId, request.UserId);
            return Errors.Organization.MemberNotFound;
        }

        if (await TeamMembershipRules.HasActiveTasksAsync(request.TeamId, assignments, inspections, ct)
            .ConfigureAwait(false))
        {
            logger.LogWarning("Cannot remove member from company team {TeamId} with active tasks", request.TeamId);
            return Errors.Organization.CannotModifyTeamWithActiveTasks;
        }

        var tracked = await members.GetByIdAsync(member.Id, ct).ConfigureAwait(false);
        if (tracked is not null)
        {
            members.Remove(tracked);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation(
                "CompanyStaff {UserId} removed from company team {TeamId} by CM {CmId}",
                request.UserId, request.TeamId, currentUser.UserId);
        }

        return Result.Success();
    }
}

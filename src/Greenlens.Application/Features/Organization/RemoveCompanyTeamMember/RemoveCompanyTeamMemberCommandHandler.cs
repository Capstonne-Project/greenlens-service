using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
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
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<RemoveCompanyTeamMemberCommandHandler> logger) : IRequestHandler<RemoveCompanyTeamMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveCompanyTeamMemberCommand request, CancellationToken ct)
    {
        // 1. Resolve CM's company
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null)
            return Errors.Organization.NotCompanyManager;

        // 2. Validate team belongs to CM's company
        var team = await teams.GetByIdAsync(request.TeamId, ct).ConfigureAwait(false);
        if (team is null)
            return Errors.Organization.TeamNotFound;

        if (team.CompanyId != staff.CompanyId)
            return Errors.Organization.TeamNotInCompany;

        // 3. Find member in team
        var member = await members.QueryAsNoTracking()
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId && m.UserId == request.UserId, ct)
            .ConfigureAwait(false);

        if (member is null)
            return Errors.Organization.MemberNotFound;

        // 4. Re-fetch tracked and remove
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

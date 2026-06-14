using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.AddCompanyTeamMember;

/// <summary>
/// CM adds a CompanyStaff user to a company team.
/// Validates: CM owns team, user is CompanyStaff in same company, not already in team.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class AddCompanyTeamMemberCommandHandler(
    ICompanyStaffRepository companyStaffRepo,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<AddCompanyTeamMemberCommandHandler> logger) : IRequestHandler<AddCompanyTeamMemberCommand, Result<AddCompanyTeamMemberResponse>>
{
    public async Task<Result<AddCompanyTeamMemberResponse>> Handle(
        AddCompanyTeamMemberCommand request,
        CancellationToken ct)
    {
        // 1. Resolve CM's company
        var cmStaff = await companyStaffRepo.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (cmStaff is null)
            return Errors.Organization.NotCompanyManager;

        // 2. Validate team exists and belongs to CM's company
        var team = await teams.GetByIdAsync(request.TeamId, ct).ConfigureAwait(false);
        if (team is null)
            return Errors.Organization.TeamNotFound;

        if (team.CompanyId != cmStaff.CompanyId)
            return Errors.Organization.TeamNotInCompany;

        // 3. Validate user exists and has role CompanyStaff
        var user = await users.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        if (user.Role != UserRole.CompanyStaff)
            return Errors.Organization.InvalidRoleForTeamMember;

        // 4. Validate user belongs to same company
        var targetStaff = await companyStaffRepo.GetByUserIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (targetStaff is null || targetStaff.CompanyId != cmStaff.CompanyId)
            return Errors.Organization.StaffNotInCompany;

        // 5. Check if already in this team
        var alreadyMember = await teamMembers.IsUserInTeamAsync(request.TeamId, request.UserId, ct)
            .ConfigureAwait(false);
        if (alreadyMember)
            return Errors.Organization.MemberAlreadyInTeam;

        // 6. Add member
        var member = TeamMember.Create(request.TeamId, request.UserId, request.IsLeader);
        teamMembers.Add(member);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "CompanyStaff {UserId} added to company team {TeamId} (leader={IsLeader}) by CM {CmId}",
            request.UserId, request.TeamId, request.IsLeader, currentUser.UserId);

        return new AddCompanyTeamMemberResponse(member.Id, member.TeamId, member.UserId, member.IsLeader);
    }
}

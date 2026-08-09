using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Inspection;

/// <summary>BR-INS-012: Only Inspection Team Leader assigned to the inspection may act.</summary>
internal static class InspectionTeamAuthorization
{
    /// <summary>
    /// Read access: Inspector → assigned team member; LEO → report ward/office; Admin → all.
    /// </summary>
    public static async Task<Error?> ValidateInspectionReadAccessAsync(
        InspectionReport inspection,
        IReportRepository reports,
        ITeamMemberRepository teamMembers,
        IUserRepository users,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.Role == UserRole.Admin.ToString())
            return null;

        if (currentUser.Role == UserRole.Inspector.ToString())
            return await ValidateTeamMemberAsync(inspection, teamMembers, currentUser, ct)
                .ConfigureAwait(false);

        if (currentUser.Role == UserRole.LEO.ToString())
        {
            var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
            if (user is null)
                return Errors.Users.UserNotFound;

            var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
            if (report is null)
                return Errors.Reports.ReportNotFound;

            return ReportReviewCandidateFilters.ValidateReportAccess(
                report, user, currentUser.Role);
        }

        return Errors.Auth.Forbidden;
    }

    /// <summary>
    /// KPI read: Inspector → own team only; LEO → inspection teams in their office; Admin → any.
    /// </summary>
    public static async Task<Error?> ValidateTeamKpiAccessAsync(
        EnvironmentalTeam team,
        User actor,
        ITeamMemberRepository teamMembers,
        ILocalOfficeRepository localOffices,
        ICompanyStaffRepository companyStaff,
        CancellationToken ct)
    {
        if (actor.Role == UserRole.Admin)
            return null;

        if (team.TeamType != TeamType.Inspection)
            return Errors.Inspections.TeamNotFound;

        if (actor.Role == UserRole.Inspector)
        {
            var member = await teamMembers.GetByUserIdAsync(actor.Id, ct).ConfigureAwait(false);
            if (member is null || member.TeamId != team.Id)
                return Errors.Inspections.NotAssignedToYourTeam;

            return null;
        }

        if (actor.Role == UserRole.LEO)
            return TeamAccessAuthorization.ValidateLeoManageCommunityTeam(team, actor);

        return Errors.Auth.Forbidden;
    }
    public static async Task<Error?> ValidateTeamLeaderAsync(
        InspectionReport inspection,
        ITeamMemberRepository teamMembers,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
            return Errors.Inspections.NotTeamLeader;

        if (inspection.AssignedTeamId != leader.TeamId)
            return Errors.Inspections.NotAssignedToYourTeam;

        return null;
    }

    /// <summary>Validate that the current user is a member (any role) of the assigned inspection team.</summary>
    public static async Task<Error?> ValidateTeamMemberAsync(
        InspectionReport inspection,
        ITeamMemberRepository teamMembers,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var member = await teamMembers.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (member is null)
            return Errors.Inspections.NotAssignedToYourTeam;

        if (inspection.AssignedTeamId != member.TeamId)
            return Errors.Inspections.NotAssignedToYourTeam;

        return null;
    }

    /// <summary>
    /// BR-INS-020, BR-ORG-012: Only the LEO assigned to the underlying report's ward/office
    /// may record a penalty payment — not the Inspection Team Leader.
    /// </summary>
    public static async Task<Error?> ValidateLeoForReportAsync(
        InspectionReport inspection,
        IReportRepository reports,
        ILocalOfficeRepository localOffices,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (!report.AssignedOfficeId.HasValue)
            return Errors.Inspections.NotAssignedLeoForReport;

        var leoOffice = await localOffices.QueryAsNoTracking()
            .FirstOrDefaultAsync(o => o.OfficerId == currentUser.UserId, ct)
            .ConfigureAwait(false);

        if (leoOffice is null || leoOffice.Id != report.AssignedOfficeId)
            return Errors.Inspections.NotAssignedLeoForReport;

        return null;
    }
}

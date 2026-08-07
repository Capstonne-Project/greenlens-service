using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Inspection;

/// <summary>BR-INS-012: Only Inspection Team Leader assigned to the inspection may act.</summary>
internal static class InspectionTeamAuthorization
{
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

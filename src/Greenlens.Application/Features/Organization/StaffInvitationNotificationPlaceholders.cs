using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization;

internal static class StaffInvitationNotificationPlaceholders
{
    internal static string FormatTargetRoleVi(UserRole role) => role switch
    {
        UserRole.Cleaner => "thành viên dọn dẹp",
        UserRole.Inspector => "thành viên thanh tra",
        _ => role.ToString()
    };

    internal static string FormatTeamClause(string? teamName) =>
        string.IsNullOrWhiteSpace(teamName) ? string.Empty : $" (đội {teamName})";

    internal static Dictionary<string, string> ForReceived(
        string inviterLabel,
        string wardName,
        UserRole targetRole,
        string? teamName) =>
        Build(inviterLabel, wardName, targetRole, teamName, memberName: null);

    internal static Dictionary<string, string> ForResponded(
        string memberName,
        string wardName,
        UserRole targetRole,
        string? teamName) =>
        Build(inviterLabel: null, wardName, targetRole, teamName, memberName);

    internal static async Task<(string WardName, string? TeamName)> ResolveContextAsync(
        Guid officeId,
        Guid? teamId,
        IApplicationDbContext db,
        IEnvironmentalTeamRepository teams,
        CancellationToken ct)
    {
        var locality = await NotificationLocalityQueries
            .FromOfficeIdAsync(db, officeId, ct)
            .ConfigureAwait(false);

        var wardName = NotificationVietnameseLabels.DisplayWardName(locality.WardName);

        string? teamName = null;
        if (teamId.HasValue)
        {
            teamName = await teams.QueryAsNoTracking()
                .Where(t => t.Id == teamId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        return (wardName, teamName);
    }

    private static Dictionary<string, string> Build(
        string? inviterLabel,
        string wardName,
        UserRole targetRole,
        string? teamName,
        string? memberName)
    {
        var placeholders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ward_name"] = wardName,
            ["office_name"] = wardName,
            ["target_role"] = FormatTargetRoleVi(targetRole),
            ["team_clause"] = FormatTeamClause(teamName)
        };

        if (inviterLabel is not null)
            placeholders["inviter_name"] = inviterLabel;

        if (memberName is not null)
            placeholders["member_name"] = memberName;

        return placeholders;
    }
}

using Greenlens.Application.Common.Interfaces.Persistence;
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
        string inviterName,
        string officeName,
        UserRole targetRole,
        string? teamName) =>
        new()
        {
            ["inviter_name"] = inviterName,
            ["office_name"] = officeName,
            ["target_role"] = FormatTargetRoleVi(targetRole),
            ["team_clause"] = FormatTeamClause(teamName)
        };

    internal static Dictionary<string, string> ForResponded(
        string memberName,
        string officeName,
        UserRole targetRole,
        string? teamName) =>
        new()
        {
            ["member_name"] = memberName,
            ["office_name"] = officeName,
            ["target_role"] = FormatTargetRoleVi(targetRole),
            ["team_clause"] = FormatTeamClause(teamName)
        };

    internal static async Task<(string OfficeName, string? TeamName)> ResolveContextAsync(
        Guid officeId,
        Guid? teamId,
        ILocalOfficeRepository offices,
        IEnvironmentalTeamRepository teams,
        CancellationToken ct)
    {
        var officeName = await offices.QueryAsNoTracking()
            .Where(o => o.Id == officeId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        string? teamName = null;
        if (teamId.HasValue)
        {
            teamName = await teams.QueryAsNoTracking()
                .Where(t => t.Id == teamId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        return (string.IsNullOrWhiteSpace(officeName) ? "phường/xã" : officeName, teamName);
    }
}

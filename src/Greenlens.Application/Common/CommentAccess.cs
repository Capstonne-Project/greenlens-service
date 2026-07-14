namespace Greenlens.Application.Common;

/// <summary>BR-CMT-001: who may comment when reporter hid their display name (BR-REP-012).</summary>
public static class CommentAccess
{
    private static readonly HashSet<string> PrivilegedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "LEO", "DEO", "Admin" };

    public static bool IsPrivilegedRole(string role) => PrivilegedRoles.Contains(role);

    public static bool CanCommentOnReport(bool hideReporterName, string role, Guid userId, Guid? reporterId)
    {
        if (!hideReporterName)
            return true;

        return IsPrivilegedRole(role) || reporterId == userId;
    }
}

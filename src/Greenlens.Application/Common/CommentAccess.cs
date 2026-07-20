namespace Greenlens.Application.Common;

/// <summary>BR-CMT-001: who may comment when reporter hid their display name (BR-REP-012).</summary>
public static class CommentAccess
{
    private static readonly HashSet<string> PrivilegedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "LEO", "DEO", "Admin" };

    /// <summary>Đội hiện trường được phép phản hồi kể cả khi report ẩn danh.</summary>
    private static readonly HashSet<string> CleanupTeamRoles =
        new(StringComparer.OrdinalIgnoreCase) { "CompanyStaff", "Cleaner" };

    public const string CleanupTeamDisplayName = "Đội xử lý";

    public static bool IsPrivilegedRole(string role) => PrivilegedRoles.Contains(role);

    public static bool IsCleanupTeamRole(string role) => CleanupTeamRoles.Contains(role);

    public static bool CanCommentOnReport(bool hideReporterName, string role, Guid userId, Guid? reporterId)
    {
        if (!hideReporterName)
            return true;

        return IsPrivilegedRole(role)
            || IsCleanupTeamRole(role)
            || reporterId == userId;
    }

    /// <summary>
    /// Citizen không thấy tên thật nhân viên đội dọn — chỉ nhãn chung.
    /// </summary>
    public static string ResolveAuthorDisplayName(string authorRole, string fullName)
    {
        if (IsCleanupTeamRole(authorRole))
            return CleanupTeamDisplayName;

        return string.IsNullOrWhiteSpace(fullName) ? "Người dùng" : fullName;
    }
}

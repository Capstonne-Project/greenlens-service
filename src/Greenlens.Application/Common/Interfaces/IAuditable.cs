namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that require audit logging (BR-ADM-010).
/// When a command implements this interface, the AuditLogBehavior will automatically
/// capture OldValues/NewValues and write an AuditLog entry after successful execution.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Human-readable action description (e.g. "UpdateUserRole", "SuspendCompany").
    /// Defaults to the command type name if not overridden.
    /// </summary>
    string AuditAction => GetType().Name.Replace("Command", "");

    /// <summary>
    /// Target entity type (e.g. "User", "Report", "Company").
    /// </summary>
    string AuditEntityType { get; }

    /// <summary>
    /// Target entity ID (e.g. the UserId being modified).
    /// </summary>
    string? AuditEntityId { get; }
}

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Persists audit log entries (BR-ADM-010).
/// Implementation lives in Infrastructure (resolves ICurrentUser, IHttpContextAccessor for IP/UA).
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string? oldValues,
        string? newValues,
        CancellationToken ct = default);
}

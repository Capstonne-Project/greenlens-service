namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Persists audit log entries (BR-ADM-010).
/// Implementation lives in Infrastructure (resolves ICurrentUser, IHttpContextAccessor for IP/UA).
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Ghi audit log ngay lập tức (tự gọi SaveChanges).
    /// Dùng cho handler không còn thao tác EF nào phía sau.
    /// </summary>
    Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string? oldValues,
        string? newValues,
        CancellationToken ct = default);

    /// <summary>
    /// Thêm audit log vào ChangeTracker nhưng KHÔNG gọi SaveChanges — caller tự commit
    /// chung với thay đổi nghiệp vụ trong cùng một lượt ghi.
    /// Dùng khi handler cần gộp audit vào đúng một transaction: tránh SaveChanges nhiều
    /// lần rồi bị ChangeTracker.Clear() của side-effect (NotificationService) làm detach
    /// entity → DbUpdateConcurrencyException giả → 409 CONCURRENCY_CONFLICT.
    /// </summary>
    Task EnqueueAsync(
        string action,
        string entityType,
        string? entityId,
        string? oldValues,
        string? newValues,
        CancellationToken ct = default);
}

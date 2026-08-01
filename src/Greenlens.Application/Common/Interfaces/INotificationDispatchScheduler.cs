namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Enqueues FCM/SMTP channel dispatch so HTTP requests are not blocked by external I/O (BR-SYS-001).
/// Implemented in Infrastructure over Hangfire.
/// </summary>
public interface INotificationDispatchScheduler
{
    /// <summary>Schedule background delivery for a persisted notification row.</summary>
    void Enqueue(Guid notificationId);
}

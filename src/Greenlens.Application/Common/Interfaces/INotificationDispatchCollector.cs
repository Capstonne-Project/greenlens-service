namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Buffers notification IDs created during an open DB transaction so Hangfire
/// dispatch runs after commit (notification row must be visible to the worker).
/// </summary>
public interface INotificationDispatchCollector
{
    void Enqueue(Guid notificationId);

    IReadOnlyList<Guid> DrainAll();

    void Clear();
}

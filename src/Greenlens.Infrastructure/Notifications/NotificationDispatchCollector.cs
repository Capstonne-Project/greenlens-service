using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Notifications;

internal sealed class NotificationDispatchCollector : INotificationDispatchCollector
{
    private readonly List<Guid> _notificationIds = [];

    public void Enqueue(Guid notificationId) => _notificationIds.Add(notificationId);

    public IReadOnlyList<Guid> DrainAll()
    {
        if (_notificationIds.Count == 0)
            return [];

        var copy = _notificationIds.ToList();
        _notificationIds.Clear();
        return copy;
    }

    public void Clear() => _notificationIds.Clear();
}

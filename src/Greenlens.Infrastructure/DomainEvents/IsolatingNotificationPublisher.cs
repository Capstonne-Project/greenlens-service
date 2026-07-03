using Greenlens.Application.Common.Interfaces;
using MediatR;
using MediatR.NotificationPublishers;

namespace Greenlens.Infrastructure.DomainEvents;

/// <summary>
/// Clears EF change tracker before each <see cref="INotificationHandler{T}"/> invocation so
/// notification persistence does not poison gamification SaveChanges (and vice versa).
/// </summary>
internal sealed class IsolatingNotificationPublisher(IChangeTrackerCleaner changeTrackerCleaner)
    : INotificationPublisher
{
    public async Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlerExecutors)
        {
            changeTrackerCleaner.ClearTrackedEntities();
            await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}

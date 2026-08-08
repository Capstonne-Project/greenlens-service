using Greenlens.Application.Common.Interfaces;
using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.DomainEvents;

/// <summary>
/// Clears EF change tracker before each <see cref="INotificationHandler{T}"/> invocation so
/// notification persistence does not poison gamification SaveChanges (and vice versa).
/// Failures in one handler do not block subsequent handlers (e.g. notification failure must not skip points).
/// </summary>
internal sealed class IsolatingNotificationPublisher(
    IChangeTrackerCleaner changeTrackerCleaner,
    ILogger<IsolatingNotificationPublisher> logger)
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
            try
            {
                await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Notification handler {HandlerType} failed for {NotificationType}",
                    handler.HandlerInstance.GetType().Name,
                    notification.GetType().Name);
            }
        }
    }
}

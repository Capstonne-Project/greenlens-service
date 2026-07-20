using Greenlens.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Notifications.Hubs;

public sealed record RealTimeNotificationPayload(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    Guid? ReferenceId,
    DateTime CreatedAt);

public interface INotificationClient
{
    Task ReceiveNotification(RealTimeNotificationPayload notification);
}

[Authorize]
public sealed class NotificationHub(ILogger<NotificationHub> logger) : Hub<INotificationClient>
{
    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        logger.LogInformation("SignalR Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        logger.LogInformation(exception, "SignalR Client disconnected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
        return base.OnDisconnectedAsync(exception);
    }
}

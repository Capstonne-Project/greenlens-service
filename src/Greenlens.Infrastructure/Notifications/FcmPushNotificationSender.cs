using FirebaseAdmin.Messaging;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Notifications;

/// <summary>
/// Sends push notifications via Firebase Cloud Messaging (BR-NTF-001).
/// Requires Firebase Admin SDK initialized (already done in DependencyInjection.cs).
/// </summary>
internal sealed class FcmPushNotificationSender(
    ILogger<FcmPushNotificationSender> logger) : IPushNotificationSender
{
    public async Task SendPushAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        var message = new Message
        {
            Token = deviceToken,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body
            },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    Sound = "default",
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                }
            }
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .SendAsync(message, ct).ConfigureAwait(false);

            logger.LogDebug("FCM push sent successfully: {MessageId}", response);
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            logger.LogWarning("FCM token expired/unregistered for token {Token}", deviceToken[..10]);
            // TODO: clear expired token from User entity
        }
    }
}

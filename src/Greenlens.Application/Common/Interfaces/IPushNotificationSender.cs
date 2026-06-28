namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Sends push notifications via Firebase Cloud Messaging (BR-NTF-001).
/// </summary>
public interface IPushNotificationSender
{
    Task SendPushAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);
}

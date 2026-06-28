using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Notifications.GetMyNotifications;
using Greenlens.Application.Features.Notifications.GetNotificationPreferences;
using Greenlens.Application.Features.Notifications.MarkAllRead;
using Greenlens.Application.Features.Notifications.MarkNotificationRead;
using Greenlens.Application.Features.Notifications.UpdateDeviceToken;
using Greenlens.Application.Features.Notifications.UpdateNotificationPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>
/// Notification management — list, read/unread, preferences, device token.
/// </summary>
/// <remarks>Implements: BR-NTF-001 (channels), BR-NTF-002 (events), BR-NTF-003 (anti-spam).</remarks>
[ApiController]
[Route("v1/notifications")]
[Authorize]
[Produces("application/json")]
[Tags("🔔 Notifications")]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    /// <summary>Get my notifications (paginated, with read/unread filter).</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List my notifications (BR-NTF-001)")]
    [SwaggerResponse(200, "Notification list", typeof(ApiResponse<GetMyNotificationsResponse>))]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        CancellationToken ct = default)
    {
        return (await sender.Send(new GetMyNotificationsQuery(page, pageSize, isRead), ct)).ToHttp();
    }

    /// <summary>Mark a single notification as read.</summary>
    [HttpPut("{id:guid}/read")]
    [SwaggerOperation(Summary = "Mark notification as read (BR-NTF-001)")]
    [SwaggerResponse(200, "Marked as read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        return (await sender.Send(new MarkNotificationReadCommand(id), ct)).ToHttpNoContent("Đã đánh dấu đã đọc.");
    }

    /// <summary>Mark all notifications as read.</summary>
    [HttpPut("read-all")]
    [SwaggerOperation(Summary = "Mark all notifications as read (BR-NTF-001)")]
    [SwaggerResponse(200, "All marked as read", typeof(ApiResponse<MarkAllReadResponse>))]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        return (await sender.Send(new MarkAllReadCommand(), ct)).ToHttp();
    }

    /// <summary>Get my notification channel preferences.</summary>
    [HttpGet("preferences")]
    [SwaggerOperation(Summary = "Get notification preferences (BR-NTF-001)")]
    [SwaggerResponse(200, "Preferences list", typeof(ApiResponse<IReadOnlyList<PreferenceItem>>))]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        return (await sender.Send(new GetNotificationPreferencesQuery(), ct)).ToHttp();
    }

    /// <summary>Update notification channel preferences (push/email per type).</summary>
    [HttpPut("preferences")]
    [SwaggerOperation(Summary = "Update notification preferences (BR-NTF-001)")]
    [SwaggerResponse(200, "Preferences updated")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesCommand command,
        CancellationToken ct)
    {
        return (await sender.Send(command, ct)).ToHttpNoContent("Đã cập nhật cài đặt thông báo.");
    }

    /// <summary>Register or update FCM device token for push notifications.</summary>
    [HttpPut("device-token")]
    [SwaggerOperation(Summary = "Register/update FCM device token (BR-NTF-001)")]
    [SwaggerResponse(200, "Token updated")]
    public async Task<IActionResult> UpdateDeviceToken(
        [FromBody] UpdateDeviceTokenCommand command,
        CancellationToken ct)
    {
        return (await sender.Send(command, ct)).ToHttpNoContent("Đã cập nhật device token.");
    }
}

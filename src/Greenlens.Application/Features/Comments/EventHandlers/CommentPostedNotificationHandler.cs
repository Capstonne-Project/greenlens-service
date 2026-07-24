using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.EventHandlers;

/// <summary>Notify report reporter when someone comments. BR-NTF-002.</summary>
internal sealed class CommentPostedNotificationHandler(
    INotificationService notificationService,
    ILogger<CommentPostedNotificationHandler> logger)
    : INotificationHandler<CommentPostedEvent>
{
    public async Task Handle(CommentPostedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Getting comment posted notification");

        if (notification.ReporterId is null || notification.ReporterId == notification.AuthorId)
        {
            logger.LogWarning("Notification not sent for reporter {UserId} or author {UserId}", notification.ReporterId, notification.AuthorId);
            return;
        }
        logger.LogInformation("Notification: new comment on report {ReportId} → notify reporter {UserId}", notification.ReportId, notification.ReporterId);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId.Value,
            NotificationType.NewComment,
            new Dictionary<string, string>
            {
                ["report_id"] = notification.ReportId.ToString()
            },
            notification.ReportId,
            ct).ConfigureAwait(false);

        logger.LogInformation("Notification sent for reporter {UserId}", notification.ReporterId);
    }
}

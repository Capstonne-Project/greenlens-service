using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.MarkNotificationRead;

/// <summary>Mark a single notification as read.</summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

/// <remarks>Implements: BR-NTF-001 (read/unread tracking).</remarks>
internal sealed class MarkNotificationReadCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepo,
    IUnitOfWork unitOfWork,
    ILogger<MarkNotificationReadCommandHandler> logger)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        logger.LogInformation("Marking notification {NotificationId} as read", request.NotificationId);

        var notification = await notificationRepo.GetByIdAsync(request.NotificationId, ct)
            .ConfigureAwait(false);

        if (notification is null)
        {
            logger.LogWarning("Notification {NotificationId} not found", request.NotificationId);
            return Errors.Notification.NotFound(request.NotificationId);
        }

        if (notification.RecipientId != currentUser.UserId)
        {
            logger.LogWarning("Notification {NotificationId} is not owned by user {UserId}", request.NotificationId, currentUser.UserId);
            return Errors.Notification.NotOwner;
        }

        notification.MarkAsRead();
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Notification {NotificationId} marked as read", request.NotificationId);

        return Result.Success();
    }
}

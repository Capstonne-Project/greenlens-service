using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Notifications.MarkNotificationRead;

/// <summary>Mark a single notification as read.</summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

/// <remarks>Implements: BR-NTF-001 (read/unread tracking).</remarks>
internal sealed class MarkNotificationReadCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await notificationRepo.GetByIdAsync(request.NotificationId, ct)
            .ConfigureAwait(false);

        if (notification is null)
            return Errors.Notification.NotFound(request.NotificationId);

        if (notification.RecipientId != currentUser.UserId)
            return Errors.Notification.NotOwner;

        notification.MarkAsRead();
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

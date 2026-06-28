using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Notifications.MarkAllRead;

/// <summary>Mark all notifications as read for the current user.</summary>
public sealed record MarkAllReadCommand : IRequest<Result<MarkAllReadResponse>>;

public sealed record MarkAllReadResponse(int MarkedCount);

/// <remarks>Implements: BR-NTF-001 (bulk read/unread management).</remarks>
internal sealed class MarkAllReadCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkAllReadCommand, Result<MarkAllReadResponse>>
{
    public async Task<Result<MarkAllReadResponse>> Handle(
        MarkAllReadCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var unreadNotifications = await notificationRepo.Query()
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return new MarkAllReadResponse(unreadNotifications.Count);
    }
}

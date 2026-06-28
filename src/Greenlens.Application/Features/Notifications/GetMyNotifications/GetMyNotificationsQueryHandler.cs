using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Notifications.GetMyNotifications;

/// <summary>
/// Lists the current user's notifications with optional read/unread filter.
/// </summary>
/// <remarks>Implements: BR-NTF-001 (notification delivery awareness).</remarks>
internal sealed class GetMyNotificationsQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepo)
    : IRequestHandler<GetMyNotificationsQuery, Result<GetMyNotificationsResponse>>
{
    public async Task<Result<GetMyNotificationsResponse>> Handle(
        GetMyNotificationsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var query = notificationRepo.Query()
            .Where(n => n.RecipientId == userId);

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var unreadCount = await notificationRepo.Query()
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationItem(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.ReferenceId,
                n.IsRead,
                n.ReadAt,
                n.CreatedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new GetMyNotificationsResponse(items, totalCount, unreadCount);
    }
}

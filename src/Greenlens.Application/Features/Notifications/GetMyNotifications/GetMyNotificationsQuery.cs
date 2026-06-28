using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Notifications.GetMyNotifications;

/// <summary>Get paginated list of notifications for the current user.</summary>
public sealed record GetMyNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsRead = null) : IRequest<Result<GetMyNotificationsResponse>>;

public sealed record GetMyNotificationsResponse(
    IReadOnlyList<NotificationItem> Items,
    int TotalCount,
    int UnreadCount);

public sealed record NotificationItem(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    Guid? ReferenceId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

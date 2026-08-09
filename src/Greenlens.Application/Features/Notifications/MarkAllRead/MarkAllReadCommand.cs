using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.MarkAllRead;

/// <summary>Mark all notifications as read for the current user.</summary>
public sealed record MarkAllReadCommand : IRequest<Result<MarkAllReadResponse>>;

public sealed record MarkAllReadResponse(int MarkedCount);

/// <remarks>Implements: BR-NTF-001 (bulk read/unread management).</remarks>
internal sealed class MarkAllReadCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepo,
    ILogger<MarkAllReadCommandHandler> logger)
    : IRequestHandler<MarkAllReadCommand, Result<MarkAllReadResponse>>
{
    public async Task<Result<MarkAllReadResponse>> Handle(
        MarkAllReadCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var markedCount = await notificationRepo
            .MarkAllAsReadForRecipientAsync(userId, ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Marked {Count} notifications as read for user {UserId}",
            markedCount,
            userId);

        return new MarkAllReadResponse(markedCount);
    }
}

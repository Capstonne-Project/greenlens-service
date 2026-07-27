using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.GetMyNotifications;

/// <summary>
/// Lists the current user's notifications with optional read/unread filter.
/// </summary>
/// <remarks>Implements: BR-NTF-001 (notification delivery awareness).</remarks>
internal sealed class GetMyNotificationsQueryHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepo,
    IReportRepository reports,
    ILogger<GetMyNotificationsQueryHandler> logger)
    : IRequestHandler<GetMyNotificationsQuery, Result<GetMyNotificationsResponse>>
{
    public async Task<Result<GetMyNotificationsResponse>> Handle(
        GetMyNotificationsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting my notifications");

        var userId = currentUser.UserId;
        logger.LogInformation("User ID: {UserId}", userId);

        var query = notificationRepo.Query()
            .Where(n => n.RecipientId == userId);

        if (request.IsRead.HasValue)
        {
            logger.LogInformation("Is read: {IsRead}", request.IsRead.Value);
            query = query.Where(n => n.IsRead == request.IsRead.Value);
        }

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
                n.CreatedAt,
                null,
                null))
            .ToListAsync(ct).ConfigureAwait(false);

        // Enrich report-linked rows with category + thumbnail (mobile list UI).
        var reportIds = items
            .Where(i => i.ReferenceId.HasValue && IsReportLinkedType(i.Type))
            .Select(i => i.ReferenceId!.Value)
            .Distinct()
            .ToList();

        if (reportIds.Count == 0)
        {
            logger.LogInformation("No report IDs found");
            return new GetMyNotificationsResponse(items, totalCount, unreadCount);
        }

        var reportMeta = await reports.QueryAsNoTracking()
            .Where(r => reportIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                CategoryName = r.Category.NameVi,
                ThumbnailUrl = r.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        logger.LogInformation("Report meta: {ReportMeta}", reportMeta);

        var enriched = items.Select(n =>
        {
            if (!n.ReferenceId.HasValue || !reportMeta.TryGetValue(n.ReferenceId.Value, out var meta))
                return n;

            return n with
            {
                CategoryName = meta.CategoryName,
                ThumbnailUrl = meta.ThumbnailUrl
            };
        }).ToList();

        logger.LogInformation("Enriched notifications: {Enriched}", enriched);

        return new GetMyNotificationsResponse(enriched, totalCount, unreadCount);
    }

    private static bool IsReportLinkedType(NotificationType type) => type is
        NotificationType.ReportStatusChanged or
        NotificationType.NewComment or
        NotificationType.NearbyReport or
        NotificationType.ReportAutoClosed or
        NotificationType.ReportOverdue or
        NotificationType.ReportUnassigned or
        NotificationType.SlaBreachWarning or
        NotificationType.DuplicateReviewNeeded or
        NotificationType.ReopenReviewNeeded or
        NotificationType.ReopenRequestDecided or
        NotificationType.PenaltyIssued;
}

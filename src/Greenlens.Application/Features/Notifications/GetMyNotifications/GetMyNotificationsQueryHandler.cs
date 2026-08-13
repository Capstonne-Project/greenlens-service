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
    IInspectionReportRepository inspections,
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
        // referenceId của type "trực tiếp Report" = Report.Id; của type "Inspection-linked"
        // (BR-INS-*) = InspectionReport.Id — cần join qua InspectionReport để lấy ReportId thật.
        var directReportIds = items
            .Where(i => i.ReferenceId.HasValue && IsReportLinkedType(i.Type))
            .Select(i => i.ReferenceId!.Value)
            .ToList();

        var inspectionIds = items
            .Where(i => i.ReferenceId.HasValue && IsInspectionLinkedType(i.Type))
            .Select(i => i.ReferenceId!.Value)
            .ToList();

        if (directReportIds.Count == 0 && inspectionIds.Count == 0)
        {
            logger.LogInformation("No report IDs found");
            return new GetMyNotificationsResponse(items, totalCount, unreadCount);
        }

        // Map InspectionId → ReportId thật, để tra reportMeta bên dưới.
        var inspectionToReportId = inspectionIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await inspections.QueryAsNoTracking()
                .Where(ir => inspectionIds.Contains(ir.Id))
                .Select(ir => new { ir.Id, ir.ReportId })
                .ToDictionaryAsync(x => x.Id, x => x.ReportId, ct)
                .ConfigureAwait(false);

        var allReportIds = directReportIds
            .Concat(inspectionToReportId.Values)
            .Distinct()
            .ToList();

        var reportMeta = await reports.QueryAsNoTracking()
            .Where(r => allReportIds.Contains(r.Id))
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
            if (!n.ReferenceId.HasValue)
                return n;

            // referenceId là InspectionId → resolve về ReportId thật trước khi tra reportMeta.
            var reportId = IsInspectionLinkedType(n.Type)
                ? (inspectionToReportId.TryGetValue(n.ReferenceId.Value, out var mapped) ? mapped : (Guid?)null)
                : n.ReferenceId.Value;

            if (!reportId.HasValue || !reportMeta.TryGetValue(reportId.Value, out var meta))
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

    /// <summary>referenceId = Report.Id thật.</summary>
    private static bool IsReportLinkedType(NotificationType type) => type is
        NotificationType.ReportStatusChanged or
        NotificationType.NewComment or
        NotificationType.NearbyReport or
        NotificationType.ReportAutoClosed or
        NotificationType.ReportOverdue or
        NotificationType.ReportUnassigned or
        NotificationType.SlaBreachWarning or
        NotificationType.SlaVerificationBreachedLeo or
        NotificationType.SlaVerificationEscalatedDeo or
        NotificationType.SlaResolutionBreached or
        NotificationType.InspectionClosedNoViolation or
        NotificationType.PenaltyPaymentOverdue or
        NotificationType.CleanupProgressStale or
        NotificationType.CleanupTaskAssigned or
        NotificationType.CleanupTaskAccepted or
        NotificationType.CleanupTaskDeclined or
        NotificationType.CleanupProgressUpdated or
        NotificationType.CleanupTaskCompleted or
        NotificationType.CleanupTeamCheckedIn or
        NotificationType.CleanupBeforeImagesUploaded or
        NotificationType.ReportClosedByCitizen or
        NotificationType.CompanyReportDispatched or
        NotificationType.CompanyTeamAssigned or
        NotificationType.DuplicateReviewNeeded or
        NotificationType.ViolationRecurrenceReviewNeeded or
        NotificationType.ReopenReviewNeeded or
        NotificationType.ReopenRequestDecided or
        NotificationType.ReportVerificationNeeded or
        NotificationType.PenaltyIssued;

    /// <summary>referenceId = InspectionReport.Id — cần join qua InspectionReport để lấy ReportId thật.</summary>
    private static bool IsInspectionLinkedType(NotificationType type) => type is
        NotificationType.SlaInspectionBreached or
        NotificationType.InspectionTaskAssigned or
        NotificationType.InspectionTaskDeclined or
        NotificationType.InspectionTaskAccepted or
        NotificationType.InspectionProgressUpdated or
        NotificationType.InspectionTaskCompleted or
        NotificationType.InspectionPenaltyPaidAndClosed;
}

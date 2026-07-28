using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.RequestReopenReport;

/// <summary>
/// Citizen requests reopen of a Resolved report with reason and evidence.
/// Report stays Resolved until LEO approves. Rating is not required (BR-REP-018).
/// </summary>
/// <remarks>
/// Implements: BR-REP-015 (7-day window, max 1 approved reopen, reason + ≥1 image),
/// BR-REP-001 (owned storage URLs), BR-NTF-002 (notify LEO).
/// </remarks>
public sealed class RequestReopenReportCommandHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IApplicationDbContext db,
    IFileStorageService fileStorage,
    INotificationService notifications,
    IOfficerRecipientQuery officerRecipients,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RequestReopenReportCommandHandler> logger) : IRequestHandler<RequestReopenReportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestReopenReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.ReporterId != currentUser.UserId)
        {
            logger.LogWarning("User {UserId} is not reporter of report {ReportId}", currentUser.UserId, request.ReportId);
            return Errors.Reports.NotReporter;
        }

        var utcNow = DateTime.UtcNow;
        var eligibilityError = ReopenRequestEligibility.ValidateCitizenCanRequest(report, utcNow);
        if (eligibilityError is not null)
        {
            logger.LogWarning(
                "Citizen reopen request blocked for report {ReportId}: {ErrorCode}",
                report.Id,
                eligibilityError.Code);
            return eligibilityError;
        }

        if (request.ImageUrls.Count == 0)
        {
            logger.LogWarning("No image urls provided for report {ReportId}", report.Id);
            return Errors.Reports.ReopenEvidenceRequired;
        }

        foreach (var url in request.ImageUrls)
        {
            if (!fileStorage.IsOwnedPublicUrl(url))
            {
                logger.LogWarning("Invalid storage url {Url} for report {ReportId}", url, report.Id);
                return Errors.Media.InvalidStorageUrl;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.VideoUrl)
            && !fileStorage.IsOwnedPublicUrl(request.VideoUrl!))
        {
            logger.LogWarning("Invalid storage url {Url} for report {ReportId}", request.VideoUrl!, report.Id);
            return Errors.Media.InvalidStorageUrl;
        }

        var reopenRequest = ReportReopenRequest.Create(
            report.Id,
            currentUser.UserId,
            request.Reason);

        db.Set<ReportReopenRequest>().Add(reopenRequest);
        report.MarkPendingReopenRequest();

        var mediaItems = new List<ReportMedia>();
        foreach (var url in request.ImageUrls)
        {
            mediaItems.Add(ReportMedia.Create(
                report.Id,
                MediaType.ReopenEvidence,
                url,
                "image/jpeg",
                0,
                currentUser.UserId,
                reopenRequestId: reopenRequest.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.VideoUrl))
        {
            mediaItems.Add(ReportMedia.Create(
                report.Id,
                MediaType.Video,
                request.VideoUrl!,
                "video/mp4",
                0,
                currentUser.UserId,
                reopenRequestId: reopenRequest.Id));
        }

        if (mediaItems.Count > 0)
        {
            logger.LogInformation("Adding {MediaItemCount} media items to report {ReportId}", mediaItems.Count, report.Id);
            reportMedia.AddRange(mediaItems);
        }

        try
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
            {
                logger.LogWarning(
                    "Concurrent reopen request blocked for report {ReportId}: {ErrorCode}",
                    report.Id,
                    mapped.Code);
                return mapped;
            }

            throw;
        }

        await NotifyLeoReviewersAsync(report, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Reopen request {RequestId} created for report {ReportId} by citizen {UserId}",
            reopenRequest.Id, report.Id, currentUser.UserId);

        return reopenRequest.Id;
    }

    private async Task NotifyLeoReviewersAsync(Report report, CancellationToken ct)
    {
        if (report.AssignedOfficeId is null)
        {
            logger.LogWarning("Report {ReportId} has no assigned office", report.Id);
            return;
        }

        var reviewerIds = await officerRecipients
            .GetLeoIdsByOfficeAsync(report.AssignedOfficeId.Value, ct)
            .ConfigureAwait(false);

        var placeholders = NotificationPlaceholders.ForReopenReview(report.Code);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, report.Id, ct)
            .ConfigureAwait(false);

        foreach (var reviewerId in reviewerIds)
        {
            await notifications.SendFromTemplateAsync(
                reviewerId,
                NotificationType.ReopenReviewNeeded,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }
    }
}

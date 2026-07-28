using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.ConfirmDuplicate;

/// <summary>
/// LEO confirms a possible duplicate: merge the report into a primary, move its images onto
/// the primary, increment the primary's reporter count, and award the duplicate reporter
/// +50% of base report points (via domain event).
/// </summary>
/// <remarks>
/// Implements: BR-REP-031 (LEO makes the final duplicate decision),
/// BR-REP-032 (primary must be Verified/InProgress; duplicate may be Submitted;
/// link to primary, merge images + comments, +50% points, +1 reporter count).
/// </remarks>
public sealed class ConfirmDuplicateCommandHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IReportStatusHistoryRepository statusHistory,
    IApplicationDbContext db,
    ISender sender,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ConfirmDuplicateCommandHandler> logger) : IRequestHandler<ConfirmDuplicateCommand, Result>
{
    public async Task<Result> Handle(ConfirmDuplicateCommand request, CancellationToken ct)
    {
        logger.LogInformation("Confirming duplicate for report {ReportId}", request.ReportId);

        if (request.ReportId == request.PrimaryReportId)
        {
            logger.LogWarning("Report {ReportId} cannot merge into self", request.ReportId);
            return Errors.Reports.CannotMergeIntoSelf;
        }

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        var primary = await reports.GetByIdAsync(request.PrimaryReportId, ct).ConfigureAwait(false);
        if (primary is null)
        {
            logger.LogWarning("Primary report not found for ID {PrimaryReportId}", request.PrimaryReportId);
            return Errors.Reports.PrimaryReportNotFound;
        }

        // BR-REP-032: primary must be verified before any duplicate can merge into it.
        if (primary.Status is not (ReportStatus.Verified or ReportStatus.InProgress or ReportStatus.Reopened))
        {
            logger.LogWarning(
                "Primary report {PrimaryReportId} must be Verified or InProgress (current: {Status})",
                primary.Id, primary.Status);
            return Errors.Reports.InvalidStatusTransition;
        }

        // Duplicate side: Submitted (and Verified) may merge; reports already in cleanup or terminal states may not.
        if (report.Status is ReportStatus.Duplicate or ReportStatus.Rejected
            or ReportStatus.InProgress or ReportStatus.Resolved or ReportStatus.Closed)
        {
            logger.LogWarning("Report {ReportId} is not valid for duplicate (status {Status})", request.ReportId, report.Status);
            return Errors.Reports.InvalidStatusTransition;
        }

        var fromStatus = report.Status;

        // BR-REP-032: merge images from duplicate into primary before marking status.
        var mediaToMerge = await reportMedia.Query()
            .Where(m => m.ReportId == report.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var media in mediaToMerge)
            media.ReassignToReport(primary.Id);

        // BR-REP-032: merge comments into primary
        var commentsToMerge = await db.Set<Comment>()
            .Where(c => c.ReportId == report.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var comment in commentsToMerge)
            comment.ReassignToReport(primary.Id);

        report.MarkDuplicate(primary.Id); // raises ReportMarkedDuplicateEvent → points + notification
        primary.IncrementReporterCount();

        statusHistory.Add(ReportStatusHistory.Create(
            report.Id,
            fromStatus,
            ReportStatus.Duplicate,
            currentUser.UserId,
            $"LEO confirmed duplicate of {primary.Code}; merged {mediaToMerge.Count} media"));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        if (primary.ReporterId.HasValue)
        {
            await sender.Send(new CheckBadgesCommand(primary.ReporterId.Value), ct)
                .ConfigureAwait(false);
        }

        logger.LogInformation(
            "Report {ReportId} confirmed duplicate of {PrimaryId} by {UserId}; merged {MediaCount} media",
            report.Id, primary.Id, currentUser.UserId, mediaToMerge.Count);

        return Result.Success();
    }
}

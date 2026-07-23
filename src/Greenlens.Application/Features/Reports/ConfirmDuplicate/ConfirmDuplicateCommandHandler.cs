using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
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
/// BR-REP-032 (link to primary, merge images + comments, +50% points, +1 reporter count).
/// </remarks>
public sealed class ConfirmDuplicateCommandHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IReportStatusHistoryRepository statusHistory,
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ConfirmDuplicateCommandHandler> logger) : IRequestHandler<ConfirmDuplicateCommand, Result>
{
    public async Task<Result> Handle(ConfirmDuplicateCommand request, CancellationToken ct)
    {
        if (request.ReportId == request.PrimaryReportId)
            return Errors.Reports.CannotMergeIntoSelf;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var primary = await reports.GetByIdAsync(request.PrimaryReportId, ct).ConfigureAwait(false);
        if (primary is null)
            return Errors.Reports.PrimaryReportNotFound;

        // BR-REP-032: report must be verified before it can be confirmed as duplicate.
        // Already-Duplicate or Rejected reports cannot be merged again.
        if (report.Status is ReportStatus.Submitted or ReportStatus.Duplicate or ReportStatus.Rejected)
            return Errors.Reports.InvalidStatusTransition;

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

        logger.LogInformation(
            "Report {ReportId} confirmed duplicate of {PrimaryId} by {UserId}; merged {MediaCount} media",
            report.Id, primary.Id, currentUser.UserId, mediaToMerge.Count);

        return Result.Success();
    }
}

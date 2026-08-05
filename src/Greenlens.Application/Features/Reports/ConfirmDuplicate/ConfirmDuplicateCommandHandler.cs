using System.Text.Json;
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
/// LEO confirms a possible duplicate: link to primary, merge comments, increment reporter count,
/// and award the duplicate reporter +50% of base report points (via domain event).
/// Duplicate report media stays on the duplicate record.
/// </summary>
/// <remarks>
/// Implements: BR-REP-031 (LEO makes the final duplicate decision),
/// BR-REP-032 (primary must be Verified/InProgress; duplicate may be Submitted;
/// link to primary, merge comments, +50% points, +1 reporter count; media not merged),
/// BR-ADM-010 (audit log).
/// </remarks>
public sealed class ConfirmDuplicateCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    IApplicationDbContext db,
    ISender sender,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
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

        if (primary.Status is not (ReportStatus.Verified or ReportStatus.InProgress or ReportStatus.Reopened))
        {
            logger.LogWarning(
                "Primary report {PrimaryReportId} must be Verified or InProgress (current: {Status})",
                primary.Id, primary.Status);
            return Errors.Reports.InvalidStatusTransition;
        }

        if (report.Status is ReportStatus.Duplicate or ReportStatus.Rejected
            or ReportStatus.InProgress or ReportStatus.Resolved or ReportStatus.Closed)
        {
            logger.LogWarning("Report {ReportId} is not valid for duplicate (status {Status})", request.ReportId, report.Status);
            return Errors.Reports.InvalidStatusTransition;
        }

        var fromStatus = report.Status;

        var commentsToMerge = await db.Set<Comment>()
            .Where(c => c.ReportId == report.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var comment in commentsToMerge)
            comment.ReassignToReport(primary.Id);

        report.MarkDuplicate(primary.Id);
        primary.IncrementReporterCount();

        statusHistory.Add(ReportStatusHistory.Create(
            report.Id,
            fromStatus,
            ReportStatus.Duplicate,
            currentUser.UserId,
            $"LEO confirmed duplicate of {primary.Code}"));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "ConfirmDuplicate",
            "Report",
            report.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { status = fromStatus.ToString() }),
            newValues: JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                primaryReportId = request.PrimaryReportId,
                mergedCommentCount = commentsToMerge.Count
            }),
            ct).ConfigureAwait(false);

        if (primary.ReporterId.HasValue)
        {
            await sender.Send(new CheckBadgesCommand(primary.ReporterId.Value), ct)
                .ConfigureAwait(false);
        }

        logger.LogInformation(
            "Report {ReportId} confirmed duplicate of {PrimaryId} by {UserId}",
            report.Id, primary.Id, currentUser.UserId);

        return Result.Success();
    }
}

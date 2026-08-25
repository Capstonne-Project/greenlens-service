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

namespace Greenlens.Application.Features.Reports.FlagReport;

/// <summary>
/// A citizen flags a report. When a report accumulates 3+ distinct flags of the same type,
/// the responsible LEO(s) are notified to review it.
/// </summary>
/// <remarks>
/// Implements: BR-REP-033 (≥3 different flags → notify LEO). One flag per (report, user, type)
/// is enforced by a unique index on report_flags.
/// </remarks>
public sealed class FlagReportCommandHandler(
    IReportRepository reports,
    IApplicationDbContext db,
    ICurrentUser currentUser,
    INotificationService notifications,
    IOfficerRecipientQuery officerRecipients,
    IUnitOfWork uow,
    ISystemSettingsProvider systemSettings,
    ILogger<FlagReportCommandHandler> logger) : IRequestHandler<FlagReportCommand, Result>
{
    public async Task<Result> Handle(FlagReportCommand request, CancellationToken ct)
    {
        logger.LogInformation("Flagging report {ReportId} as {FlagType}", request.ReportId, request.Type);

        if (!currentUser.IsAuthenticated)
            return Errors.Reports.LoginRequired;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }
        // BR-REP-033: cannot flag your own report.
        if (report.ReporterId == currentUser.UserId)
        {
            logger.LogWarning("Report {ReportId} is reporter", request.ReportId);
            return Errors.Reports.CannotFlagOwnReport;
        }

        var alreadyFlagged = await db.Set<ReportFlag>()
            .AnyAsync(
                f => f.ReportId == request.ReportId
                     && f.FlaggerId == currentUser.UserId
                     && f.FlagType == request.Type,
                ct)
            .ConfigureAwait(false);
        if (alreadyFlagged)
        {
            logger.LogWarning("Report {ReportId} is already flagged as {FlagType}", request.ReportId, request.Type);
            return Errors.Reports.AlreadyFlagged;
        }

        db.Set<ReportFlag>().Add(
            ReportFlag.Create(request.ReportId, currentUser.UserId, request.Type, request.Reason));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var flagCount = await db.Set<ReportFlag>()
            .CountAsync(f => f.ReportId == request.ReportId && f.FlagType == request.Type, ct)
            .ConfigureAwait(false);

        var notifyThreshold = ReportSystemSettings.FlagNotifyThreshold(systemSettings);
        if (flagCount >= notifyThreshold)
        {
            logger.LogWarning("Report {ReportId} has {Count} flags of type {FlagType} → notifying reviewers", request.ReportId, flagCount, request.Type);
            await NotifyReviewersAsync(report, request.Type, flagCount, ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Report {ReportId} flagged as {FlagType} by {UserId} (total {Count})",
            report.Id, request.Type, currentUser.UserId, flagCount);

        return Result.Success();
    }

    private async Task NotifyReviewersAsync(Report report, FlagType type, int count, CancellationToken ct)
    {
        logger.LogInformation("Notifying reviewers for report {ReportId} of type {FlagType} with {Count} flags", report.Id, type, count);

        if (report.AssignedOfficeId is null)
            return;

        var reviewerIds = await officerRecipients
            .GetLeoIdsByOfficeAsync(report.AssignedOfficeId.Value, ct)
            .ConfigureAwait(false);

        logger.LogInformation("Found {Count} reviewers for report {ReportId} of type {FlagType}", reviewerIds.Count, report.Id, type);

        var placeholders = NotificationPlaceholders.ForDuplicateReviewFromFlags(report.Code, type, count);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, report.Id, ct)
            .ConfigureAwait(false);

        foreach (var reviewerId in reviewerIds)
        {
            await notifications.SendFromTemplateAsync(
                reviewerId,
                NotificationType.DuplicateReviewNeeded,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation("Notified {Count} reviewers for report {ReportId} of type {FlagType}", reviewerIds.Count, report.Id, type);
    }
}

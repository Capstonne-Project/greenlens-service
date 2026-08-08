using System.Text.Json;
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

namespace Greenlens.Application.Features.Reports.ApproveReopenRequest;

/// <summary>LEO approves citizen reopen request. Resolved → Reopened (BR-REP-015).</summary>
/// <remarks>
/// Implements: BR-REP-015, BR-REP-020, BR-REP-021, BR-ORG-012, BR-NTF-002, BR-ADM-010.
/// </remarks>
public sealed class ApproveReopenRequestCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    IApplicationDbContext db,
    IUserRepository users,
    INotificationService notifications,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<ApproveReopenRequestCommandHandler> logger) : IRequestHandler<ApproveReopenRequestCommand, Result>
{
    public async Task<Result> Handle(ApproveReopenRequestCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var reopenRequest = await db.Set<ReportReopenRequest>()
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ReportId == request.ReportId, ct)
            .ConfigureAwait(false);

        if (reopenRequest is null)
        {
            logger.LogWarning("Reopen request not found for ID {RequestId}", request.RequestId);
            return Errors.Reports.ReopenRequestNotFound;
        }

        if (reopenRequest.Status != ReopenRequestStatus.Pending)
        {
            logger.LogWarning("Reopen request {RequestId} is not pending", request.RequestId);
            return Errors.Reports.ReopenRequestNotPending;
        }

        var scopeError = await ReopenRequestAuthorization.ValidateLeoScopeAsync(
            report, users, currentUser, ct).ConfigureAwait(false);
        if (scopeError is not null)
        {
            logger.LogWarning("Invalid LEO scope for report {ReportId}", report.Id);
            return scopeError;
        }

        if (!report.ApproveReopen(currentUser.UserId))
        {
            if (report.Status != ReportStatus.Resolved)
            {
                logger.LogWarning("Report {ReportId} is not Resolved for reopen approval", report.Id);
                return Errors.Reports.ReportNotResolvedForReopenApproval;
            }

            if (report.ReopenedCount >= Report.MaxApprovedReopens)
            {
                logger.LogWarning("Reopen limit reached for report {ReportId}", report.Id);
                return Errors.Reports.ReopenLimitReached;
            }

            logger.LogWarning("Invalid status transition for report {ReportId}", report.Id);
            return Errors.Reports.InvalidStatusTransition;
        }

        reopenRequest.Approve(currentUser.UserId);

        statusHistory.Add(ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Resolved,
            ReportStatus.Reopened,
            currentUser.UserId));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "ApproveReopenRequest",
            "Report",
            report.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { status = ReportStatus.Resolved.ToString() }),
            newValues: JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                requestId = request.RequestId
            }),
            ct).ConfigureAwait(false);

        if (report.ReporterId.HasValue)
        {
            var placeholders = NotificationPlaceholders.ForReopenDecided(report.Code, approved: true);
            placeholders = await NotificationLocalityQueries
                .EnrichFromReportIdAsync(db, placeholders, report.Id, ct)
                .ConfigureAwait(false);

            await notifications.SendFromTemplateAsync(
                report.ReporterId.Value,
                NotificationType.ReopenRequestDecided,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "LEO {UserId} approved reopen request {RequestId} for report {ReportId}",
            currentUser.UserId, request.RequestId, report.Id);

        return Result.Success();
    }
}

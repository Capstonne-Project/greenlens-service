using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.RejectReport;

/// <summary>
/// LEO rejects a report — report is re-queued to Department Common Queue.
/// </summary>
/// <remarks>
/// Implements: BR-ORG-015 (re-assign khi LEO reject, reason ≥ 20 chars),
/// BR-ADM-010 (audit log).
/// Report stays Submitted, AssignedOfficeId cleared → DEO picks up from common queue.
/// </remarks>
public sealed class RejectReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<RejectReportCommandHandler> logger) : IRequestHandler<RejectReportCommand, Result>
{
    public async Task<Result> Handle(RejectReportCommand request, CancellationToken ct)
    {
        logger.LogInformation("Rejecting report {ReportId}", request.ReportId);

        // Validate rejection reason length (BR-REP-022 / BR-ORG-015)
        if (request.Reason.Length < 20)
        {
            logger.LogWarning("Reason is too short for report {ReportId}", request.ReportId);
            return Errors.Reports.ReasonTooShort;
        }

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.Status != ReportStatus.Submitted)
        {
            logger.LogWarning("Report {ReportId} is not in a valid status for rejection", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        // BR-OFF-004: conflict of interest — cannot reject own report
        if (report.ReporterId == currentUser.UserId)
        {
            logger.LogWarning("Report {ReportId} is rejected by the reporter {UserId}", request.ReportId, currentUser.UserId);
            return Errors.Reports.ConflictOfInterest;
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            assignedOfficeId = report.AssignedOfficeId
        });

        // BR-ORG-015: Reject re-queues to Department — status stays Submitted,
        // AssignedOfficeId cleared so DEO sees it in common queue
        report.Reject(request.Reason);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Submitted,   // stays Submitted
            ReportStatus.Submitted,   // re-queued (not terminal Rejected)
            currentUser.UserId,
            $"LEO rejected — re-queued to Department: {request.Reason}");

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "RejectReport",
            "Report",
            report.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                assignedOfficeId = report.AssignedOfficeId,
                reasonLength = request.Reason.Length
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} rejected by LEO {UserId}, re-queued to Department",
            report.Id, currentUser.UserId);

        return Result.Success();
    }
}

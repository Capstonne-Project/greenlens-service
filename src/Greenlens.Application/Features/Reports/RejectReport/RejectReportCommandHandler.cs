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
/// LEO rejects a report — terminal Rejected status with reason.
/// </summary>
/// <remarks>
/// Implements: BR-REP-022 (reason ≥ 20 chars), BR-ADM-010 (audit log).
/// </remarks>
public sealed class RejectReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    ISystemSettingsProvider systemSettings,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<RejectReportCommandHandler> logger) : IRequestHandler<RejectReportCommand, Result>
{
    public async Task<Result> Handle(RejectReportCommand request, CancellationToken ct)
    {
        var (rejectMin, _) = ModuleSystemSettings.ValidationReasonLengths(systemSettings);

        logger.LogInformation("Rejecting report {ReportId}", request.ReportId);

        // Validate rejection reason length (BR-REP-022 / BR-ORG-015)
        if (request.Reason.Length < rejectMin)
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
            status = report.Status.ToString()
        });

        report.Reject(request.Reason);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Submitted,
            ReportStatus.Rejected,
            currentUser.UserId,
            request.Reason);

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
                reasonLength = request.Reason.Length
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} rejected by LEO {UserId}",
            report.Id, currentUser.UserId);

        return Result.Success();
    }
}

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
/// Implements: BR-ORG-015 (re-assign khi LEO reject, reason ≥ 20 chars).
/// Report stays Submitted, AssignedOfficeId cleared → DEO picks up from common queue.
/// </remarks>
public sealed class RejectReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RejectReportCommandHandler> logger) : IRequestHandler<RejectReportCommand, Result>
{
    public async Task<Result> Handle(RejectReportCommand request, CancellationToken ct)
    {
        // Validate rejection reason length (BR-REP-022 / BR-ORG-015)
        if (request.Reason.Length < 20)
            return Errors.Reports.ReasonTooShort;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Submitted)
            return Errors.Reports.InvalidStatusTransition;

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

        logger.LogInformation(
            "Report {ReportId} rejected by LEO {UserId}, re-queued to Department",
            report.Id, currentUser.UserId);

        return Result.Success();
    }
}

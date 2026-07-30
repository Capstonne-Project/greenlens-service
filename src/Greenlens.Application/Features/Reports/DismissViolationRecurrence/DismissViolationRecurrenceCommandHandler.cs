using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DismissViolationRecurrence;

/// <summary>
/// LEO clears a violation-recurrence suspicion after review (ordinary repeat pollution, not a violator case).
/// </summary>
/// <remarks>Implements: BR-REP-034, BR-ADM-010.</remarks>
public sealed class DismissViolationRecurrenceCommandHandler(
    IReportRepository reports,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<DismissViolationRecurrenceCommandHandler> logger)
    : IRequestHandler<DismissViolationRecurrenceCommand, Result>
{
    public async Task<Result> Handle(DismissViolationRecurrenceCommand request, CancellationToken ct)
    {
        logger.LogInformation("Dismissing violation recurrence for report {ReportId}", request.ReportId);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (!report.IsSuspectedViolationRecurrence)
        {
            logger.LogWarning("Report {ReportId} is not flagged for violation recurrence", request.ReportId);
            return Errors.Reports.NotSuspectedViolationRecurrence;
        }

        report.DismissViolationRecurrence();
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "DismissViolationRecurrence",
            "Report",
            report.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { isSuspectedViolationRecurrence = true }),
            newValues: JsonSerializer.Serialize(new { isSuspectedViolationRecurrence = false }),
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} violation-recurrence flag dismissed by {UserId}",
            report.Id, currentUser.UserId);

        return Result.Success();
    }
}

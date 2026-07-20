using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.ReopenReport;

/// <summary>Citizen reopens. Max 2 times. BR-REP-015.</summary>
public sealed class ReopenReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ReopenReportCommandHandler> logger) : IRequestHandler<ReopenReportCommand, Result>
{
    public async Task<Result> Handle(ReopenReportCommand request, CancellationToken ct)
    {
        // Find report
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.ReporterId != currentUser.UserId)
            return Errors.Reports.NotReporter;

        // BR-REP-015: Check 7-day reopen window (distinct error from limit reached)
        if (report.Status == ReportStatus.Resolved
            && report.ResolvedAt.HasValue
            && DateTime.UtcNow - report.ResolvedAt.Value > TimeSpan.FromDays(7))
            return Errors.Reports.ReopenWindowExpired;

        // Attempt reopen — max 2 times (BR-REP-015)
        if (!report.TryReopen())
        {
            logger.LogWarning("Reopen limit reached for report {ReportId}", report.Id);
            return Errors.Reports.ReopenLimitReached;
        }

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Resolved,
            ReportStatus.InProgress,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} reopened by citizen {UserId}", report.Id, currentUser.UserId);

        return Result.Success();
    }
}

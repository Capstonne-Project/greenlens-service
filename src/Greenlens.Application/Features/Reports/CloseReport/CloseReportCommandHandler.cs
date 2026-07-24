using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.CloseReport;

/// <summary>Citizen confirms satisfaction or auto-close. BR-REP-016.</summary>
public sealed class CloseReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<CloseReportCommandHandler> logger) : IRequestHandler<CloseReportCommand, Result>
{
    public async Task<Result> Handle(CloseReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        // Validate status — only Resolved can be closed
        if (report.Status != ReportStatus.Resolved)
        {
            logger.LogWarning("Report {ReportId} is not resolved", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        if (report.ReporterId != currentUser.UserId)
        {
            logger.LogWarning("Report {ReportId} is not reporter", request.ReportId);
            return Errors.Reports.NotReporter;
        }

        var fromStatus = report.Status;
        report.Close();
    
        var history = ReportStatusHistory.Create(
            report.Id,
            fromStatus,
            ReportStatus.Closed,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} closed from {FromStatus}", report.Id, fromStatus);

        return Result.Success();
    }
}

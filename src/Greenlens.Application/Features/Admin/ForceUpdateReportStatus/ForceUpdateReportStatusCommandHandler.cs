using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.ForceUpdateReportStatus;

public sealed class ForceUpdateReportStatusCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ForceUpdateReportStatusCommandHandler> logger) : IRequestHandler<ForceUpdateReportStatusCommand, Result>
{
    public async Task<Result> Handle(ForceUpdateReportStatusCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var fromStatus = report.Status;

        // Admin can force any status via domain method
        report.ForceStatus(request.NewStatus);

        var history = ReportStatusHistory.Create(
            report.Id, fromStatus, request.NewStatus,
            currentUser.UserId,
            $"[ADMIN] {request.Reason}");

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogWarning("[ADMIN] Report {ReportId} force-updated {FromStatus} → {ToStatus} by {AdminId}: {Reason}",
            report.Id, fromStatus, request.NewStatus, currentUser.UserId, request.Reason);

        return Result.Success();
    }
}

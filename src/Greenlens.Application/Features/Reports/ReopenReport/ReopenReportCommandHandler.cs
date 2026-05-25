using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.ReopenReport;

/// <summary>Citizen reopens. Max 2 times. BR-REP-015.</summary>
public sealed class ReopenReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<ReopenReportCommand, Result>
{
    public async Task<Result> Handle(ReopenReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (!report.TryReopen())
            return Errors.Reports.ReopenLimitReached;

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Resolved,
            ReportStatus.InProgress,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

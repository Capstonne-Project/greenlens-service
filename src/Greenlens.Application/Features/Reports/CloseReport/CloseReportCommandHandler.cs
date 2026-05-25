using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.CloseReport;

/// <summary>Citizen confirms satisfaction or auto-close. BR-REP-016.</summary>
public sealed class CloseReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<CloseReportCommand, Result>
{
    public async Task<Result> Handle(CloseReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status is not (ReportStatus.Resolved or ReportStatus.PenaltyIssued))
            return Errors.Reports.InvalidStatusTransition;

        var fromStatus = report.Status;
        report.Close();

        var history = ReportStatusHistory.Create(
            report.Id,
            fromStatus,
            ReportStatus.Closed,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.RejectReport;

/// <summary>Officer rejects a report. BR-REP-022: reason ≥ 20 chars.</summary>
public sealed class RejectReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<RejectReportCommand, Result>
{
    public async Task<Result> Handle(RejectReportCommand request, CancellationToken ct)
    {
        if (request.Reason.Length < 20)
            return Errors.Reports.ReasonTooShort;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Submitted)
            return Errors.Reports.InvalidStatusTransition;

        report.Reject(request.Reason);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Submitted,
            ReportStatus.Rejected,
            currentUser.UserId,
            request.Reason);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

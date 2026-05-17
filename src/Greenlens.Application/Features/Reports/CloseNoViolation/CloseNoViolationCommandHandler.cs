using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.CloseNoViolation;

/// <summary>Close report with no violation. BR-INS-013: reason ≥ 50 chars.</summary>
public sealed class CloseNoViolationCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<CloseNoViolationCommand, Result>
{
    public async Task<Result> Handle(CloseNoViolationCommand request, CancellationToken ct)
    {
        if (request.Reason.Length < 50)
            return Errors.Reports.ReasonTooShort50;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        report.CloseNoViolation();

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.InProgress,
            ReportStatus.ClosedNoViolation,
            currentUser.UserId,
            request.Reason);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

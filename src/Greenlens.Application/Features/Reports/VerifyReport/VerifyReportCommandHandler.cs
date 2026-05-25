using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.VerifyReport;

/// <summary>
/// Officer verifies a submitted report. Checks conflict of interest (BR-OFF-004).
/// </summary>
public sealed class VerifyReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<VerifyReportCommand, Result>
{
    public async Task<Result> Handle(VerifyReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Submitted)
            return Errors.Reports.InvalidStatusTransition;

        // BR-OFF-004: conflict of interest
        if (report.ReporterId == currentUser.UserId)
            return Errors.Reports.ConflictOfInterest;

        report.Verify(currentUser.UserId, request.OverrideSeverity, request.OverrideCategoryId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Submitted,
            ReportStatus.Verified,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

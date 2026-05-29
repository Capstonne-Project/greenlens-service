using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DispatchReport;

/// <summary>
/// DEO dispatches a verified report to a target LocalOffice.
/// Validates: report is Verified, DEO belongs to the same province as the target office,
/// and the target office is onboarded.
/// </summary>
public sealed class DispatchReportCommandHandler(
    IReportRepository reports,
    ILocalOfficeRepository localOffices,
    IReportStatusHistoryRepository statusHistory,
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DispatchReportCommandHandler> logger) : IRequestHandler<DispatchReportCommand, Result>
{
    public async Task<Result> Handle(DispatchReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Verified)
            return Errors.Reports.InvalidStatusTransition;

        // Load DEO user to verify province scope
        var deo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (deo is null || !deo.DepartmentId.HasValue)
            return Errors.Users.UserNotFound;

        // Load target office with department
        var targetOffice = await localOffices.QueryAsNoTracking()
            .Include(o => o.Department)
            .FirstOrDefaultAsync(o => o.Id == request.TargetLocalOfficeId, ct)
            .ConfigureAwait(false);

        if (targetOffice is null)
            return Errors.Organization.OfficeNotFound;

        if (!targetOffice.IsOnboarded)
            return Errors.Organization.OfficeNotOnboarded;

        // DEO can only dispatch within their province
        if (targetOffice.DepartmentId != deo.DepartmentId.Value)
            return Errors.Reports.DispatchOutsideProvince;

        // Dispatch: Verified → Dispatched
        report.Dispatch(currentUser.UserId, targetOffice.Id, targetOffice.OfficerId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Verified,
            ReportStatus.Dispatched,
            currentUser.UserId,
            request.Note);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} dispatched to office {OfficeId} by DEO {UserId}",
            report.Id, request.TargetLocalOfficeId, currentUser.UserId);

        return Result.Success();
    }
}

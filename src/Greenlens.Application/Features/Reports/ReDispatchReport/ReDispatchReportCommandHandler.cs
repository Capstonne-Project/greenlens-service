using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.ReDispatchReport;

/// <summary>
/// DEO re-dispatches a task to a different ward. Only possible when status is Dispatched.
/// </summary>
public sealed class ReDispatchReportCommandHandler(
    IReportRepository reports,
    ILocalOfficeRepository localOffices,
    IReportStatusHistoryRepository statusHistory,
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ReDispatchReportCommandHandler> logger) : IRequestHandler<ReDispatchReportCommand, Result>
{
    public async Task<Result> Handle(ReDispatchReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Dispatched)
            return Errors.Reports.InvalidStatusTransition;

        var deo = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (deo is null || !deo.DepartmentId.HasValue)
            return Errors.Users.UserNotFound;

        var newOffice = await localOffices.QueryAsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.NewLocalOfficeId, ct)
            .ConfigureAwait(false);

        if (newOffice is null)
            return Errors.Organization.OfficeNotFound;

        if (!newOffice.IsOnboarded)
            return Errors.Organization.OfficeNotOnboarded;

        if (newOffice.DepartmentId != deo.DepartmentId.Value)
            return Errors.Reports.DispatchOutsideProvince;

        var oldOfficeId = report.AssignedOfficeId;
        report.ReDispatch(currentUser.UserId, newOffice.Id, newOffice.OfficerId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Dispatched,
            ReportStatus.Dispatched,
            currentUser.UserId,
            request.Note ?? $"Re-dispatch from office {oldOfficeId}");

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} re-dispatched from office {OldOfficeId} to {NewOfficeId}",
            report.Id, oldOfficeId, newOffice.Id);

        return Result.Success();
    }
}

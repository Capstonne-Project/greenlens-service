using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.AcceptAssignment;

/// <summary>
/// Team accepts assignment: assignment Assigned → InProgress, report Assigned → InProgress.
/// First team to accept triggers the report status change; subsequent teams in multi-team
/// assignments only update their own assignment status.
/// </summary>
public sealed class AcceptAssignmentCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<AcceptAssignmentCommand, Result>
{
    public async Task<Result> Handle(AcceptAssignmentCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Assigned)
            return Errors.Reports.InvalidStatusTransition;

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.Assigned)
            return Errors.Reports.InvalidStatusTransition;

        assignment.Start();

        // First acceptance → transition report to InProgress
        report.Accept();

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Assigned,
            ReportStatus.InProgress,
            currentUser.UserId);

        statusHistory.Add(history);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

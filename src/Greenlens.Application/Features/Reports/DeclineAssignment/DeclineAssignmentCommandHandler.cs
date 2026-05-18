using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.DeclineAssignment;

/// <summary>
/// Team declines within 2h window. BR-CLN-007, BR-INS-003.
/// If ALL assignments are declined → report reverts to Verified so officer can re-assign.
/// </summary>
public sealed class DeclineAssignmentCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<DeclineAssignmentCommand, Result>
{
    public async Task<Result> Handle(DeclineAssignmentCommand request, CancellationToken ct)
    {
        if (request.Reason.Length < 20)
            return Errors.Reports.ReasonTooShort;

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

        // BR-CLN-007, BR-INS-003: 2h window
        if ((DateTime.UtcNow - assignment.AssignedAt).TotalHours > 2)
            return Errors.Reports.DeclineWindowExpired;

        assignment.Decline(request.Reason);

        // If ALL assignments are now declined → revert report to Verified
        var allDeclined = reportAssignments
            .All(a => a.TeamId == request.TeamId
                ? true  // current one just declined
                : a.Status == AssignmentStatus.Declined);

        if (allDeclined)
        {
            report.RevertToVerified();

            var history = ReportStatusHistory.Create(
                report.Id,
                ReportStatus.Assigned,
                ReportStatus.Verified,
                currentUser.UserId);

            statusHistory.Add(history);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

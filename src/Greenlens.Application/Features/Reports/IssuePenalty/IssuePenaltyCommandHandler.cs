using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.IssuePenalty;

/// <summary>
/// Inspection Team Leader issues penalty. BR-INS-012.
/// Marks team's assignment as completed; report transitions to PenaltyIssued
/// only when ALL active assignments are completed.
/// </summary>
public sealed class IssuePenaltyCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<IssuePenaltyCommand, Result>
{
    public async Task<Result> Handle(IssuePenaltyCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        // Find this team's assignment
        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);
        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status == AssignmentStatus.Completed)
            return Errors.Reports.InvalidStatusTransition;

        // Auto-start if still Assigned
        if (assignment.Status == AssignmentStatus.Assigned)
            assignment.Start();

        assignment.Complete();

        // Check if ALL active assignments are completed
        var activeAssignments = reportAssignments
            .Where(a => a.Status != AssignmentStatus.Declined)
            .ToList();

        var allCompleted = activeAssignments.All(a => a.Status == AssignmentStatus.Completed);

        if (allCompleted)
        {
            report.IssuePenalty();

            var history = ReportStatusHistory.Create(
                report.Id,
                ReportStatus.InProgress,
                ReportStatus.PenaltyIssued,
                currentUser.UserId);

            statusHistory.Add(history);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

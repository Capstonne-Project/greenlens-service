using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.ResolveReport;

/// <summary>
/// Cleanup Team marks their assignment as completed. BR-CLN-005: ≥ 2 after images.
/// When ALL assignments are completed → report transitions to Resolved.
/// </summary>
public sealed class ResolveReportCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<ResolveReportCommand, Result>
{
    public async Task<Result> Handle(ResolveReportCommand request, CancellationToken ct)
    {
        // BR-CLN-005: at least 2 after images
        if (request.AfterImageUrls.Count < 2)
            return Errors.Reports.InsufficientAfterImages;

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

        if (assignment.Status != AssignmentStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        assignment.Complete();

        // Check if ALL active assignments (non-declined) are completed
        var activeAssignments = reportAssignments
            .Where(a => a.Status != AssignmentStatus.Declined)
            .ToList();

        var allCompleted = activeAssignments.All(a => a.Status == AssignmentStatus.Completed);

        if (allCompleted)
        {
            // All teams done → transition report to Resolved
            report.Resolve();

            var history = ReportStatusHistory.Create(
                report.Id,
                ReportStatus.InProgress,
                ReportStatus.Resolved,
                currentUser.UserId);

            statusHistory.Add(history);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

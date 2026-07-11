using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.UpdateCleanupProgress;

/// <summary>
/// BR-CLN-004: Update cleanup progress. Must be InProgress.
/// </summary>
public sealed class UpdateCleanupProgressCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateCleanupProgressCommandHandler> logger)
    : IRequestHandler<UpdateCleanupProgressCommand, Result>
{
    public async Task<Result> Handle(UpdateCleanupProgressCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.InProgress)
            return Errors.Cleanup.AssignmentNotInProgress;

        assignment.UpdateProgress(request.Percent, request.Note, currentUser.UserId);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Team {TeamId} updated progress for report {ReportId}: {Percent}%",
            request.TeamId, report.Id, request.Percent);

        return Result.Success();
    }
}

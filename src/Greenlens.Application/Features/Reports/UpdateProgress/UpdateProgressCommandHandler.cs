using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.UpdateProgress;

/// <summary>
/// Team leader updates cleanup progress (percent + note). Assignment must be InProgress.
/// Does NOT change report or assignment status.
/// </summary>
public sealed class UpdateProgressCommandHandler(
    IReportAssignmentRepository assignments,
    IUnitOfWork uow) : IRequestHandler<UpdateProgressCommand, Result>
{
    public async Task<Result> Handle(UpdateProgressCommand request, CancellationToken ct)
    {
        if (request.ProgressPercent is < 0 or > 100)
            return Errors.Reports.InvalidProgressPercent;

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.InProgress)
            return Errors.Reports.AssignmentNotInProgress;

        assignment.UpdateProgress(request.ProgressPercent, request.ProgressNote);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

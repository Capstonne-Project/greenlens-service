using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.AcceptAssignment;

/// <summary>
/// Team leader accepts the assignment. Assigned → InProgress. StartedAt set here.
/// Implements: BR-CLN-001, BR-INS-001.
/// </summary>
public sealed class AcceptAssignmentCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<AcceptAssignmentCommand, Result>
{
    public async Task<Result> Handle(AcceptAssignmentCommand request, CancellationToken ct)
    {
        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
            return Errors.Reports.NotTeamLeader;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == leader.TeamId);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.Assigned)
            return Errors.Reports.InvalidStatusTransition;

        assignment.Accept();

        // Mark report StartedAt on first accept
        report.MarkStarted();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

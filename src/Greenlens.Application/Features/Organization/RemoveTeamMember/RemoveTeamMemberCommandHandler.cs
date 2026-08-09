using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.RemoveTeamMember;

/// <summary>Removes a member (including leader) from a community environmental team.</summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed class RemoveTeamMemberCommandHandler(
    ITeamMemberRepository members,
    IReportAssignmentRepository assignments,
    IInspectionReportRepository inspections,
    IUnitOfWork uow,
    ILogger<RemoveTeamMemberCommandHandler> logger) : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken ct)
    {
        logger.LogInformation("Removing team member for user {UserId}", request.UserId);

        var member = await members.QueryAsNoTracking()
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId && m.UserId == request.UserId, ct)
            .ConfigureAwait(false);

        if (member is null)
        {
            logger.LogWarning("Member not found for team ID {TeamId} and user ID {UserId}", request.TeamId, request.UserId);
            return Errors.Organization.MemberNotFound;
        }

        if (await TeamMembershipRules.HasActiveTasksAsync(request.TeamId, assignments, inspections, ct)
            .ConfigureAwait(false))
        {
            logger.LogWarning("Cannot remove member from team {TeamId} with active tasks", request.TeamId);
            return Errors.Organization.CannotModifyTeamWithActiveTasks;
        }

        var tracked = await members.GetByIdAsync(member.Id, ct).ConfigureAwait(false);
        if (tracked is not null)
        {
            members.Remove(tracked);
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation("User {UserId} removed from team {TeamId}",
                request.UserId, request.TeamId);
        }

        return Result.Success();
    }
}

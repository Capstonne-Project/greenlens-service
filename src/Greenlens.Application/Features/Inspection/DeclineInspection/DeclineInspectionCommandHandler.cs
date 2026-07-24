using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.DeclineInspection;

/// <summary>
/// BR-INS-003: Inspection Team declines within 24h.
/// Clears AssignedTeamId, keeps Draft status so LEO can re-assign.
/// </summary>
public sealed class DeclineInspectionCommandHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DeclineInspectionCommandHandler> logger)
    : IRequestHandler<DeclineInspectionCommand, Result>
{
    public async Task<Result> Handle(DeclineInspectionCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting decline inspection");

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        if (inspection.Status != InspectionStatus.Draft)
        {
            logger.LogWarning("Inspection {InspectionId} is not in draft status", request.InspectionId);
            return Errors.Inspections.InvalidStatusTransition;
        }
        if (inspection.AssignedTeamId is null)
        {
            logger.LogWarning("Inspection {InspectionId} has no team assigned", request.InspectionId);
            return Errors.Inspections.NoTeamAssigned;
        }
        // Verify current user is a member of the assigned team
        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("Team member validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }

        // BR-INS-003: 24h window from creation
        if ((DateTime.UtcNow - inspection.CreatedAt).TotalHours > 24)
        {
            logger.LogWarning("Decline window expired for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.DeclineWindowExpired;
        }

        // Clear team assignment, keep Draft for LEO re-assignment
        var declinedTeamId = inspection.AssignedTeamId;
        inspection.ClearTeam();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Inspection {InspectionId} declined by team {TeamId}: {Reason}",
            inspection.Id, declinedTeamId, request.Reason);

        return Result.Success();
    }
}

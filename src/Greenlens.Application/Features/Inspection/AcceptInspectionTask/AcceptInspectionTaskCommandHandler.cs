using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.AcceptInspectionTask;

/// <summary>
/// BR-INS-033: Team member accepts inspection task. Draft → InProgress.
/// </summary>
/// <remarks>Implements: BR-INS-003 (accept vs decline), BR-INS-033.</remarks>
public sealed class AcceptInspectionTaskCommandHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AcceptInspectionTaskCommandHandler> logger)
    : IRequestHandler<AcceptInspectionTaskCommand, Result>
{
    public async Task<Result> Handle(AcceptInspectionTaskCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for ID {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        var result = inspection.AcceptTask(currentUser.UserId);
        if (result.IsFailure)
        {
            logger.LogWarning("Accept task failed for inspection {InspectionId}", request.InspectionId);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Inspection {InspectionId} accepted by user {UserId}",
            inspection.Id, currentUser.UserId);

        return Result.Success();
    }
}

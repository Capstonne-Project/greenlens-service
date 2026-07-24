using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.CloseInspection;

/// <summary>Close inspection after full payment (Paid → Closed).</summary>
public sealed class CloseInspectionCommandHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<CloseInspectionCommandHandler> logger)
    : IRequestHandler<CloseInspectionCommand, Result>
{
    public async Task<Result> Handle(CloseInspectionCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("Team leader validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }
        var result = inspection.Close(request.Reason);
        if (result.IsFailure)
        {
            logger.LogWarning("Close inspection failed for inspection {InspectionId}. Result: {Result}", request.InspectionId, result.Error);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("InspectionReport {InspectionId} CLOSED by {UserId}", request.InspectionId, currentUser.UserId);
        return Result.Success();
    }
}

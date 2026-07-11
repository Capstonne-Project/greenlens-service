using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionProgress;

/// <summary>
/// BR-INS-031: Update inspection progress while InProgress.
/// Must be updated ≥ 1 time/day. >24h → warning; >48h → escalate flag.
/// </summary>
public sealed class UpdateInspectionProgressCommandHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateInspectionProgressCommandHandler> logger)
    : IRequestHandler<UpdateInspectionProgressCommand, Result>
{
    public async Task<Result> Handle(UpdateInspectionProgressCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        // Must be team member
        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        var result = inspection.UpdateProgress(request.Percent, request.Note);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Inspection {InspectionId} progress updated to {Percent}%",
            inspection.Id, request.Percent);

        return Result.Success();
    }
}

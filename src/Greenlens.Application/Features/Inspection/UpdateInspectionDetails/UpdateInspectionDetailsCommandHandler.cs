using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionDetails;

/// <summary>BR-INS-010: Inspector fills in field investigation details.</summary>
public sealed class UpdateInspectionDetailsCommandHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateInspectionDetailsCommandHandler> logger)
    : IRequestHandler<UpdateInspectionDetailsCommand, Result>
{
    public async Task<Result> Handle(UpdateInspectionDetailsCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        var result = inspection.UpdateDetails(
            request.ViolationDescription,
            request.ViolatorName,
            request.ViolatorAddress,
            request.ViolatorIdentity);

        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("InspectionReport {Id} details updated", request.InspectionId);
        return Result.Success();
    }
}

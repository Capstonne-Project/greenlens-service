using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionDetails;

/// <summary>
/// BR-INS-010: Inspector fills in field investigation details.
/// BR-INS-022: Optionally links a ViolatingEntity for repeat offender tracking.
/// </summary>
public sealed class UpdateInspectionDetailsCommandHandler(
    IInspectionReportRepository inspections,
    IViolatingEntityRepository violatingEntities,
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

        // BR-INS-010/022: Link ViolatingEntity if provided
        if (request.ViolatingEntityId is not null)
        {
            var veExists = await violatingEntities
                .ExistsAsync(ve => ve.Id == request.ViolatingEntityId.Value, ct)
                .ConfigureAwait(false);
            if (!veExists)
                return Errors.Inspections.ViolatingEntityNotFound;

            var linkResult = inspection.LinkViolatingEntity(request.ViolatingEntityId.Value);
            if (linkResult.IsFailure) return linkResult;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "InspectionReport {Id} details updated, violatingEntityId={VeId}",
            request.InspectionId, request.ViolatingEntityId);
        return Result.Success();
    }
}


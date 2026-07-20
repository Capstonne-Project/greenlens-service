using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionDetails;

/// <summary>
/// BR-INS-010: Inspector updates violation details (biên bản hiện trường) while in Draft.
/// Optionally links a ViolatingEntity for repeat offender tracking (BR-INS-022).
/// </summary>
public sealed record UpdateInspectionDetailsCommand(
    Guid InspectionId,
    string? ViolationDescription,
    string? ViolatorName,
    string? ViolatorAddress,
    string? ViolatorIdentity,
    Guid? ViolatingEntityId = null) : IRequest<Result>;

using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionDetails;

/// <summary>BR-INS-010: Inspector updates violation details (biên bản hiện trường) while in Draft.</summary>
public sealed record UpdateInspectionDetailsCommand(
    Guid InspectionId,
    string? ViolationDescription,
    string? ViolatorName,
    string? ViolatorAddress,
    string? ViolatorIdentity) : IRequest<Result>;

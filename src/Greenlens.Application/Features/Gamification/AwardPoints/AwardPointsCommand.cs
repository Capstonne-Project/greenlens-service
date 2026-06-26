using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Gamification.AwardPoints;

/// <summary>
/// Award or deduct points for a user based on a report action.
/// </summary>
/// <remarks>Implements: BR-GAM-001. Typically dispatched by DomainEvent handlers, not directly by controllers.</remarks>
public sealed record AwardPointsCommand(
    Guid UserId,
    int Points,
    PointReason Reason,
    Guid? ReportId) : IRequest<Result<AwardPointsResponse>>;

public sealed record AwardPointsResponse(
    int PointsAwarded,
    int TotalPoints,
    int Level,
    bool WasSkipped);

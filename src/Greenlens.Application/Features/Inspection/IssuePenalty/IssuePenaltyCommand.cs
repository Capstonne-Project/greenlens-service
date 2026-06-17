using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Inspection.IssuePenalty;

/// <summary>
/// BR-INS-012: Team Leader issues penalty decision.
/// BR-INS-011: Classify violation level.
/// BR-INS-022: Auto-check repeat offender.
/// </summary>
public sealed record IssuePenaltyCommand(
    Guid InspectionId,
    ViolationLevel ViolationLevel,
    decimal PenaltyAmount,
    string DecisionNumber,
    int PaymentDueDays = 10,
    string? AdditionalMeasures = null) : IRequest<Result>;

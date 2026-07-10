using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.CreatePenaltyFramework;

/// <summary>
/// Create a new penalty framework entry for a violation level + pollution category.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed record CreatePenaltyFrameworkCommand(
    Guid CategoryId,
    ViolationLevel ViolationLevel,
    decimal MinAmount,
    decimal MaxAmount,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo) : IRequest<Result<CreatePenaltyFrameworkResponse>>;

public sealed record CreatePenaltyFrameworkResponse(
    Guid Id,
    Guid CategoryId,
    string ViolationLevel,
    decimal MinAmount,
    decimal MaxAmount,
    DateTime EffectiveFrom);

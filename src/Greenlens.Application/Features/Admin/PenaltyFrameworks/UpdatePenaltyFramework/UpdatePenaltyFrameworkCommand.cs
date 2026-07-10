using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.UpdatePenaltyFramework;

/// <summary>
/// Update an existing penalty framework entry (amounts, dates).
/// </summary>
/// <remarks>Implements: BR-ADM-008 — changes do not affect already-issued decisions.</remarks>
public sealed record UpdatePenaltyFrameworkCommand(
    Guid Id,
    decimal MinAmount,
    decimal MaxAmount,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo) : IRequest<Result>;

using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.CloseNoViolation;

/// <summary>BR-INS-013: No violation found — close with reason ≥ 50 chars.</summary>
public sealed record CloseNoViolationCommand(
    Guid InspectionId,
    string Reason) : IRequest<Result>;

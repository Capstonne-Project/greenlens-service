using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.DeclineInspection;

/// <summary>
/// BR-INS-003: Inspection Team declines task within 24h window, with reason.
/// After 24h, task is auto-accepted.
/// </summary>
public sealed record DeclineInspectionCommand(
    Guid InspectionId,
    string Reason) : IRequest<Result>;

using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.CheckInInspection;

/// <summary>
/// BR-INS-004: Inspector checks in at the site.
/// GPS must be ≤ 200m from report location (PostGIS).
/// Transitions: Draft → InProgress.
/// </summary>
public sealed record CheckInInspectionCommand(
    Guid InspectionId,
    decimal Latitude,
    decimal Longitude,
    string? Note = null) : IRequest<Result>;

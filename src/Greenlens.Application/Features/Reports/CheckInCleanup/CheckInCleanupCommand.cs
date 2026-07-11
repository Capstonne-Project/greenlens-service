using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.CheckInCleanup;

/// <summary>
/// BR-CLN-002/003: Team checks in at the cleanup site.
/// GPS must be ≤ 200m from report location (PostGIS).
/// Transitions assignment: Assigned → InProgress.
/// </summary>
public sealed record CheckInCleanupCommand(
    Guid ReportId,
    Guid TeamId,
    decimal Latitude,
    decimal Longitude,
    string? Note = null) : IRequest<Result>;

using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.CheckInCommunityCleanup;

/// <summary>
/// Citizen or Leader checks in on-site. Draft BR-CMU-007: GPS ≤ 200m.
/// <paramref name="Reason"/> is optional — when the user is outside the 200m radius, supplying
/// a reason (≥ 20 chars) allows the check-in to proceed anyway (draft: out-of-range override).
/// </summary>
public sealed record CheckInCommunityCleanupCommand(
    Guid EventId,
    decimal Latitude,
    decimal Longitude,
    string? Reason = null) : IRequest<Result>;

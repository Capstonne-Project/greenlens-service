using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.CheckInCommunityCleanup;

/// <summary>Citizen or Leader checks in on-site. Draft BR-CMU-007: GPS ≤ 200m.</summary>
public sealed record CheckInCommunityCleanupCommand(
    Guid EventId,
    decimal Latitude,
    decimal Longitude) : IRequest<Result>;

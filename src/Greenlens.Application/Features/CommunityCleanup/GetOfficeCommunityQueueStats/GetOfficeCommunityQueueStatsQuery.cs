using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetOfficeCommunityQueueStats;

/// <summary>Aggregate counters for the LEO's community-cleanup queue, scoped to their office.</summary>
public sealed record GetOfficeCommunityQueueStatsQuery : IRequest<Result<CommunityCleanupQueueStatsResponse>>;

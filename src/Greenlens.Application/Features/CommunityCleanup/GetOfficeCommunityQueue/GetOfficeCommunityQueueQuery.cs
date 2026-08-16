using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetOfficeCommunityQueue;

/// <summary>LEO's community-cleanup queue, scoped to their office. All statuses by default.</summary>
public sealed record GetOfficeCommunityQueueQuery(
    int Page,
    int PageSize,
    IReadOnlyList<CommunityCleanupStatus>? Statuses = null) : IRequest<Result<CommunityCleanupListResponse>>;

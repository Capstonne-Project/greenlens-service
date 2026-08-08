using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetOfficeCommunityQueue;

/// <summary>LEO's community-cleanup queue, default PendingVerification, scoped to their office.</summary>
public sealed record GetOfficeCommunityQueueQuery(
    int Page,
    int PageSize,
    CommunityCleanupStatus? Status) : IRequest<Result<CommunityCleanupListResponse>>;

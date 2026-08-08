using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.CreateCommunityCleanup;

/// <summary>
/// LEO opens a Community Cleanup program on a Verified report and appoints a Cleaner as Leader.
/// Draft BR-CMU-001, BR-CMU-002 (docs/community-cleanup-feature-spec.md §7.1).
/// </summary>
public sealed record CreateCommunityCleanupCommand(
    Guid ReportId,
    string Title,
    string? Description,
    Guid LeaderUserId,
    DateTime StartsAt,
    DateTime? EndsAt,
    DateTime? JoinClosesAt,
    int MaxParticipants,
    string? MeetingNote,
    decimal? MeetingLatitude,
    decimal? MeetingLongitude) : IRequest<Result<CommunityCleanupEventDetailResponse>>;

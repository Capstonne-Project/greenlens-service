using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Raised when a LEO opens a Community Cleanup program on a Verified report — citizens can now Join.
/// Draft rule (docs/community-cleanup-feature-spec.md, BR-CMU-001/BR-CMU-014 "OpenedNearby" catalog entry).
/// </summary>
public sealed record CommunityCleanupOpenedEvent(Guid EventId, Guid ReportId, string Title) : IDomainEvent;

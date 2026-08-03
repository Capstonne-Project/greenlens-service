using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Raised when a LEO opens a Community Cleanup program on a Verified report and appoints a Leader.
/// Notifies the appointed Leader only (BR-CMU-002) — the program itself is discoverable on the
/// public map (BR-MAP-*) via a "Cộng đồng" marker, so no broadcast to all Citizens is sent.
/// </summary>
public sealed record CommunityCleanupOpenedEvent(Guid EventId, Guid ReportId, string Title, Guid LeaderUserId) : IDomainEvent;

/// <summary>
/// Raised when a LEO approves a Community Cleanup's verification (PendingVerification → Completed).
/// Consumed by gamification to award points/badges to checked-in participants.
/// </summary>
public sealed record CommunityCleanupCompletedEvent(Guid EventId, Guid ReportId) : IDomainEvent;

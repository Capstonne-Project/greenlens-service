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

/// <summary>Raised when the Leader starts the cleanup on-site. Notifies other participants + the LEO.</summary>
public sealed record CommunityCleanupStartedEvent(Guid EventId, Guid ReportId, string Title, Guid LeaderUserId, Guid LeoId) : IDomainEvent;

/// <summary>Raised when the Leader posts a progress update. Notifies other participants + the LEO.</summary>
public sealed record CommunityCleanupProgressUpdatedEvent(Guid EventId, string Title, int ProgressPercent, Guid LeaderUserId, Guid LeoId) : IDomainEvent;

/// <summary>Raised when the Leader submits completion evidence for LEO review.</summary>
public sealed record CommunityCleanupVerificationSubmittedEvent(Guid EventId, Guid ReportId, string Title, Guid LeoId) : IDomainEvent;

/// <summary>Raised when the LEO rejects submitted evidence. Notifies the Leader.</summary>
public sealed record CommunityCleanupVerificationRejectedEvent(Guid EventId, string Title, Guid LeaderUserId, string Reason) : IDomainEvent;

/// <summary>Raised alongside CommunityCleanupCompletedEvent, for the Notifications module to notify participants + the LEO (kept separate from the gamification-only event).</summary>
public sealed record CommunityCleanupVerifiedEvent(Guid EventId, string Title, Guid LeoId) : IDomainEvent;

/// <summary>Raised when the Leader checks in on-site. Notifies other participants + the LEO that the Leader has arrived.</summary>
public sealed record CommunityCleanupLeaderCheckedInEvent(Guid EventId, string Title, Guid LeaderUserId, Guid LeoId) : IDomainEvent;

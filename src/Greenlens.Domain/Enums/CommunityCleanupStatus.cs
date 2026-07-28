namespace Greenlens.Domain.Enums;

/// <summary>
/// Lifecycle of a Community Cleanup event (docs/community-cleanup-feature-spec.md §4.1).
/// </summary>
public enum CommunityCleanupStatus
{
    OpenForJoin,
    JoinClosed,
    InProgress,
    PendingVerification,
    Completed,
    Cancelled
}

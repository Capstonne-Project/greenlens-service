namespace Greenlens.Domain.Enums;

/// <summary>
/// Status of a citizen/leader's participation in a Community Cleanup event
/// (docs/community-cleanup-feature-spec.md §4.2). No Pending/Rejected in MVP — Join = start immediately.
/// </summary>
public enum CommunityCleanupParticipantStatus
{
    Joined,
    CheckedIn,
    Withdrawn,
    NoShow
}

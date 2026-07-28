using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// A user's participation row in a Community Cleanup event — the Leader has one row (Role = Leader),
/// every joined Citizen has one row (Role = Member).
/// </summary>
/// <remarks>Draft rules: BR-CMU-004..007 (docs/community-cleanup-feature-spec.md §4.2, §5.2).</remarks>
public sealed class CommunityCleanupParticipant : BaseEntity
{
    private CommunityCleanupParticipant() { } // EF Core constructor

    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public CommunityCleanupParticipantStatus Status { get; private set; } = CommunityCleanupParticipantStatus.Joined;
    public CommunityCleanupParticipantRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public decimal? CheckInLatitude { get; private set; }
    public decimal? CheckInLongitude { get; private set; }

    // ── Navigation ──
    public CommunityCleanupEvent? Event { get; private set; }
    public User? User { get; private set; }

    /// <summary>BR-CMU-005: Join = Member immediately (or Leader row created at event creation).</summary>
    public static CommunityCleanupParticipant Create(Guid eventId, Guid userId, CommunityCleanupParticipantRole role)
    {
        return new CommunityCleanupParticipant
        {
            EventId = eventId,
            UserId = userId,
            Role = role,
            Status = CommunityCleanupParticipantStatus.Joined,
            JoinedAt = DateTime.UtcNow
        };
    }

    /// <summary>BR-CMU-007: Check-in at the site (GPS distance validated by the caller).</summary>
    public void CheckIn(decimal latitude, decimal longitude)
    {
        if (Status != CommunityCleanupParticipantStatus.Joined)
            throw new InvalidOperationException($"Cannot check in from status {Status}. Must be Joined.");

        Status = CommunityCleanupParticipantStatus.CheckedIn;
        CheckedInAt = DateTime.UtcNow;
        CheckInLatitude = latitude;
        CheckInLongitude = longitude;
    }

    /// <summary>BR-CMU-006: Withdraw before check-in (event-status window enforced by the caller).</summary>
    public void Withdraw()
    {
        if (Status != CommunityCleanupParticipantStatus.Joined)
            throw new InvalidOperationException($"Cannot withdraw from status {Status}. Must be Joined.");

        Status = CommunityCleanupParticipantStatus.Withdrawn;
    }

    /// <summary>System-forced withdrawal when the event is cancelled — allowed from Joined or CheckedIn.</summary>
    public void ForceWithdraw()
    {
        if (Status is not (CommunityCleanupParticipantStatus.Joined or CommunityCleanupParticipantStatus.CheckedIn))
            throw new InvalidOperationException($"Cannot force-withdraw from status {Status}.");

        Status = CommunityCleanupParticipantStatus.Withdrawn;
    }

    /// <summary>Leader/LEO marks a no-show after StartsAt.</summary>
    public void MarkNoShow()
    {
        if (Status is not (CommunityCleanupParticipantStatus.Joined or CommunityCleanupParticipantStatus.CheckedIn))
            throw new InvalidOperationException($"Cannot mark no-show from status {Status}.");

        Status = CommunityCleanupParticipantStatus.NoShow;
    }
}

using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// A crowdsourced cleanup program opened by a LEO on a Verified report. A single Cleaner
/// (the Leader) runs the cleanup on the ground while Citizens Join (= Vote) to participate.
/// Replaces ReportAssignment/company dispatch for the report while active.
/// </summary>
/// <remarks>
/// Draft rules (docs/community-cleanup-feature-spec.md, BR IDs pending official assignment):
/// BR-CMU-001..015. State machine: §4.1.
/// </remarks>
public sealed class CommunityCleanupEvent : SoftDeletableEntity
{
    private CommunityCleanupEvent() { } // EF Core constructor

    public Guid ReportId { get; private set; }
    public Guid CreatedByLeoId { get; private set; }
    public Guid LeaderUserId { get; private set; }
    public Guid LeaderTeamId { get; private set; }
    public CommunityCleanupStatus Status { get; private set; } = CommunityCleanupStatus.OpenForJoin;

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public DateTime JoinOpensAt { get; private set; }
    public DateTime? JoinClosesAt { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }

    public int MaxParticipants { get; private set; } = 50;

    public string? MeetingNote { get; private set; }
    public decimal? MeetingLatitude { get; private set; }
    public decimal? MeetingLongitude { get; private set; }

    public int ProgressPercent { get; private set; }
    public string? ProgressNote { get; private set; }
    public DateTime? ProgressUpdatedAt { get; private set; }

    public DateTime? SubmittedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public Guid? VerifiedByLeoId { get; private set; }
    public string? RejectionReason { get; private set; }

    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }

    /// <summary>Latest Facebook Page photo post id from Graph API ({pageId}_{storyId}).</summary>
    public string? FacebookPostId { get; private set; }

    /// <summary>Permalink of the latest Facebook Page share.</summary>
    public string? FacebookPageUrl { get; private set; }

    public DateTime? FacebookPageSharedAt { get; private set; }

    // ── Navigation ──
    public Report? Report { get; private set; }
    public User? CreatedByLeo { get; private set; }
    public User? LeaderUser { get; private set; }
    public EnvironmentalTeam? LeaderTeam { get; private set; }
    public ICollection<CommunityCleanupParticipant> Participants { get; private set; } = [];

    /// <summary>An event still occupies the "one active event per report" slot.</summary>
    public bool IsActive => Status is not (CommunityCleanupStatus.Completed or CommunityCleanupStatus.Cancelled);

    /// <summary>BR-CMU-001/002: LEO opens a program and appoints a Leader.</summary>
    public static CommunityCleanupEvent Create(
        Guid reportId,
        Guid createdByLeoId,
        Guid leaderUserId,
        Guid leaderTeamId,
        string title,
        string? description,
        DateTime startsAt,
        DateTime? endsAt,
        DateTime? joinClosesAt,
        int maxParticipants,
        string? meetingNote,
        decimal? meetingLatitude,
        decimal? meetingLongitude)
    {
        var ev = new CommunityCleanupEvent
        {
            ReportId = reportId,
            CreatedByLeoId = createdByLeoId,
            LeaderUserId = leaderUserId,
            LeaderTeamId = leaderTeamId,
            Title = title,
            Description = description,
            Status = CommunityCleanupStatus.OpenForJoin,
            JoinOpensAt = DateTime.UtcNow,
            JoinClosesAt = joinClosesAt,
            StartsAt = startsAt,
            EndsAt = endsAt,
            MaxParticipants = maxParticipants,
            MeetingNote = meetingNote,
            MeetingLatitude = meetingLatitude,
            MeetingLongitude = meetingLongitude,
            CreatedAt = DateTime.UtcNow
        };

        ev.AddDomainEvent(new CommunityCleanupOpenedEvent(ev.Id, reportId, title, leaderUserId));

        return ev;
    }

    /// <summary>LEO closes registration early (or job closes it at JoinClosesAt). OpenForJoin → JoinClosed.</summary>
    public void CloseJoin()
    {
        EnsureStatus(CommunityCleanupStatus.OpenForJoin);
        Status = CommunityCleanupStatus.JoinClosed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Leader starts the cleanup on-site. {OpenForJoin, JoinClosed} → InProgress.</summary>
    public void Start()
    {
        if (Status is not (CommunityCleanupStatus.OpenForJoin or CommunityCleanupStatus.JoinClosed))
            throw new InvalidOperationException(
                $"Cannot start from status {Status}. Must be OpenForJoin or JoinClosed.");

        Status = CommunityCleanupStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CommunityCleanupStartedEvent(Id, ReportId, Title, LeaderUserId, CreatedByLeoId));
    }

    /// <summary>BR-CMU-008: Leader updates progress mid-cleanup. Percent can only increase, never decrease.</summary>
    public void UpdateProgress(int percent, string? note)
    {
        EnsureStatus(CommunityCleanupStatus.InProgress);

        if (percent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be 0-100.");

        if (percent < ProgressPercent)
            throw new InvalidOperationException(
                $"Progress cannot decrease from {ProgressPercent}% to {percent}%.");

        ProgressPercent = percent;
        ProgressNote = note;
        ProgressUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CommunityCleanupProgressUpdatedEvent(Id, Title, percent, LeaderUserId, CreatedByLeoId));
    }

    /// <summary>BR-CMU-009: Leader submits evidence for LEO review. InProgress → PendingVerification.</summary>
    public void SubmitVerification()
    {
        EnsureStatus(CommunityCleanupStatus.InProgress);
        Status = CommunityCleanupStatus.PendingVerification;
        SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CommunityCleanupVerificationSubmittedEvent(Id, ReportId, Title, CreatedByLeoId));
    }

    /// <summary>BR-CMU-010: LEO approves. PendingVerification → Completed.</summary>
    public void Approve(Guid leoId)
    {
        EnsureStatus(CommunityCleanupStatus.PendingVerification);
        Status = CommunityCleanupStatus.Completed;
        VerifiedAt = DateTime.UtcNow;
        VerifiedByLeoId = leoId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CommunityCleanupCompletedEvent(Id, ReportId));
        AddDomainEvent(new CommunityCleanupVerifiedEvent(Id, Title, leoId));
    }

    /// <summary>BR-CMU-011: LEO rejects, reason ≥ 20 chars (validated by caller). PendingVerification → InProgress.</summary>
    public void Reject(string reason)
    {
        EnsureStatus(CommunityCleanupStatus.PendingVerification);
        Status = CommunityCleanupStatus.InProgress;
        RejectionReason = reason;
        SubmittedAt = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CommunityCleanupVerificationRejectedEvent(Id, Title, LeaderUserId, reason));
    }

    /// <summary>BR-CMU-012: LEO cancels. Any status except Completed → Cancelled.</summary>
    public void Cancel(string reason)
    {
        if (Status == CommunityCleanupStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed event.");

        Status = CommunityCleanupStatus.Cancelled;
        CancelReason = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Stores the latest Facebook Page share; overwritten on each successful re-share.</summary>
    public void RecordFacebookPageShare(string postId, string pageUrl)
    {
        FacebookPostId = postId;
        FacebookPageUrl = pageUrl;
        FacebookPageSharedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(CommunityCleanupStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Invalid state transition: expected {expected} but current is {Status}.");
    }
}

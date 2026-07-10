namespace Greenlens.Domain.Enums;

/// <summary>
/// Types of notification events (BR-NTF-002).
/// Used for preference toggles (BR-NTF-001) and anti-spam grouping (BR-NTF-003).
/// </summary>
public enum NotificationType
{
    ReportStatusChanged,
    NewComment,
    BadgeEarned,
    LevelUp,
    SlaBreachWarning,
    NearbyReport,
    PenaltyIssued,
    ContractExpiry,
    /// <summary>BR-REP-008: Report pending > 72h.</summary>
    ReportOverdue,
    /// <summary>BR-REP-009: Verified report unassigned > 24h.</summary>
    ReportUnassigned,
    /// <summary>BR-REP-016: Report auto-closed after 7 days.</summary>
    ReportAutoClosed
}

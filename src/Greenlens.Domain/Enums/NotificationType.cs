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
    ContractExpiry
}

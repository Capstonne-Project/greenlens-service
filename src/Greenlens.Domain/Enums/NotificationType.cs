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
    /// <summary>Generic SLA breach warning (legacy). Prefer specific SLA types below.</summary>
    SlaBreachWarning,

    /// <summary>BR-OFF-002: LEO — report exceeded 24h verification SLA.</summary>
    SlaVerificationBreachedLeo,

    /// <summary>BR-OFF-002: DEO — escalated report entered department queue.</summary>
    SlaVerificationEscalatedDeo,

    /// <summary>BR-OFF-020: LEO — report exceeded resolution SLA.</summary>
    SlaResolutionBreached,

    /// <summary>BR-INS-030: Officer — inspection penalty dossier exceeded SLA.</summary>
    SlaInspectionBreached,

    /// <summary>BR-INS-001: Inspection team member — new inspection task assigned.</summary>
    InspectionTaskAssigned,

    /// <summary>BR-INS-003: LEO — assigned inspection team declined the task.</summary>
    InspectionTaskDeclined,

    /// <summary>BR-INS-001: LEO — assigned inspection team accepted the task.</summary>
    InspectionTaskAccepted,

    /// <summary>BR-INS-033: LEO — inspection team updated field progress (checklist/evidence/arrival).</summary>
    InspectionProgressUpdated,

    /// <summary>BR-INS-012/013: LEO — inspection team completed the field task (penalty or no violation).</summary>
    InspectionTaskCompleted,

    /// <summary>BR-INS-013: Citizen — inspection closed with no violation found.</summary>
    InspectionClosedNoViolation,

    /// <summary>BR-INS-021: LEO/DEO — penalty payment deadline passed.</summary>
    PenaltyPaymentOverdue,

    /// <summary>BR-INS-020: Inspector (issuer) — LEO recorded full payment and closed the dossier.</summary>
    InspectionPenaltyPaidAndClosed,

    /// <summary>BR-CLN-004: LEO — cleanup team stale progress &gt; 48h.</summary>
    CleanupProgressStale,

    /// <summary>BR-CLN-001: Cleanup team member — new task assigned to the team.</summary>
    CleanupTaskAssigned,

    /// <summary>BR-CLN-001: LEO/CM — assigned cleanup team accepted the task.</summary>
    CleanupTaskAccepted,

    /// <summary>BR-CLN-007: LEO/CM — assigned cleanup team declined the task.</summary>
    CleanupTaskDeclined,

    /// <summary>BR-CLN-004: LEO/CM — cleanup team posted a progress update.</summary>
    CleanupProgressUpdated,

    /// <summary>BR-CLN-005: LEO/CM — cleanup team completed their assignment.</summary>
    CleanupTaskCompleted,

    NearbyReport,
    PenaltyIssued,
    /// <summary>BR-CMP-007: Contract expired — company deactivated (legacy generic).</summary>
    ContractExpiry,

    /// <summary>BR-CMP-007: Bidding company contract has expired.</summary>
    ContractExpired,

    /// <summary>BR-CMP-007: Contract expiry warning (30/7/1 days before end).</summary>
    ContractExpiryWarning,

    /// <summary>BR-CMP-005: CompanyManager — LEO dispatched a report to the company queue.</summary>
    CompanyReportDispatched,

    /// <summary>BR-REP-008: Report pending > 72h.</summary>
    ReportOverdue,
    /// <summary>BR-REP-009: Verified report unassigned > 24h.</summary>
    ReportUnassigned,
    /// <summary>BR-REP-016: Report auto-closed after 2 days.</summary>
    ReportAutoClosed,
    /// <summary>BR-REP-032/033: A report needs LEO review for possible duplicate (3+ citizen flags or AI-confirmed match).</summary>
    DuplicateReviewNeeded,

    /// <summary>BR-REP-034: New report near a recently Closed report — LEO should compare for possible violator recurrence.</summary>
    ViolationRecurrenceReviewNeeded,

    /// <summary>Draft BR-CMU-*: LEO opened a Community Cleanup program citizens can join.</summary>
    CommunityCleanupOpened,

    /// <summary>Draft BR-CMU-002: Cleaner was appointed Leader of a new Community Cleanup program.</summary>
    CommunityCleanupLeaderAssigned,

    /// <summary>Draft BR-CMU-*: Leader checked in / event started — notifies other participants.</summary>
    CommunityCleanupStarted,

    /// <summary>Draft BR-CMU-*: Leader posted a progress update (percent/photos) — notifies participants.</summary>
    CommunityCleanupProgressUpdated,

    /// <summary>Draft BR-CMU-*: Leader submitted completion evidence — notifies the LEO to review.</summary>
    CommunityCleanupVerificationSubmitted,

    /// <summary>Draft BR-CMU-*: LEO approved the cleanup as complete — notifies participants.</summary>
    CommunityCleanupVerified,

    /// <summary>Draft BR-CMU-*: LEO rejected the submitted evidence — notifies the Leader.</summary>
    CommunityCleanupVerificationRejected,

    /// <summary>Reminder ~15 minutes before a Community Cleanup's StartsAt for participants who haven't checked in.</summary>
    CommunityCleanupCheckInReminder,

    /// <summary>Progress toward a not-yet-earned badge crossed the halfway (or later) mark.</summary>
    BadgeProgressNear,

    /// <summary>BR-REP-015: Citizen submitted a reopen request with evidence.</summary>
    ReopenReviewNeeded,

    /// <summary>BR-REP-015: LEO approved or rejected a citizen reopen request.</summary>
    ReopenRequestDecided,

    /// <summary>BR-OFF-002: New Submitted report routed to LEO/DEO verification queue.</summary>
    ReportVerificationNeeded,

    /// <summary>BR-ORG-020: LEO invited Citizen to join a ward team.</summary>
    StaffInvitationReceived,

    /// <summary>BR-ORG-020/021: Citizen accepted a staff invitation.</summary>
    StaffInvitationAccepted,

    /// <summary>BR-ORG-021: Citizen declined a staff invitation.</summary>
    StaffInvitationDeclined
}

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

    /// <summary>BR-CLN-004: LEO — cleanup team stale progress &gt; 48h.</summary>
    CleanupProgressStale,

    /// <summary>BR-CLN-001: Cleanup team member — new task assigned to the team.</summary>
    CleanupTaskAssigned,

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
    /// <summary>BR-REP-016: Report auto-closed after 7 days.</summary>
    ReportAutoClosed,
    /// <summary>BR-REP-032/033: A report needs LEO review for possible duplicate (3+ citizen flags or AI-confirmed match).</summary>
    DuplicateReviewNeeded,

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

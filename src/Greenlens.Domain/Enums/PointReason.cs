namespace Greenlens.Domain.Enums;

/// <summary>
/// Reasons for awarding or deducting gamification points (BR-GAM-001).
/// </summary>
public enum PointReason
{
    /// <summary>Report verified by LEO: +10 points.</summary>
    ReportVerified,

    /// <summary>Report resolved (cleanup completed): +20 points.</summary>
    ReportResolved,

    /// <summary>Penalty issued via InspectionReport: +20 points.</summary>
    PenaltyIssued,

    /// <summary>Duplicate report merged with primary: +50% of ReportVerified points (BR-REP-032).</summary>
    DuplicateReport,

    /// <summary>Legacy — không còn cộng/trừ điểm khi reject; giữ cho lịch sử point_transactions.</summary>
    ReportRejected,

    /// <summary>BR-GAM-006: Fraud penalty — deduct all points from batch.</summary>
    FraudPenalty,

    /// <summary>Community cleanup event completed, participant checked in: +15 points.</summary>
    CommunityCleanupParticipation
}

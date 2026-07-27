using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>Raised when a citizen submits a new report for LEO verification. BR-OFF-002.</summary>
public sealed record ReportSubmittedEvent(Guid ReportId) : IDomainEvent;

/// <summary>Raised when a report is verified by LEO (Submitted → Verified).</summary>
public sealed record ReportVerifiedEvent(Guid ReportId, Guid ReporterId) : IDomainEvent;

/// <summary>Raised when a report is rejected by LEO (Submitted → Rejected).</summary>
public sealed record ReportRejectedEvent(Guid ReportId, Guid ReporterId) : IDomainEvent;

/// <summary>Raised when a report is resolved (InProgress → Resolved).</summary>
public sealed record ReportResolvedEvent(Guid ReportId, Guid ReporterId) : IDomainEvent;

/// <summary>
/// Raised when Tier 1 (geo+time+category) flags a report as a possible duplicate.
/// Triggers Tier 2 AI image compare in a background job. BR-REP-030, BR-REP-031, BR-AI-002.
/// </summary>
public sealed record ReportPossibleDuplicateFlaggedEvent(Guid ReportId, Guid CandidateReportId) : IDomainEvent;

/// <summary>
/// Raised when a report is merged into a primary as a confirmed duplicate.
/// Drives gamification award + reporter notification. BR-REP-032.
/// </summary>
public sealed record ReportMarkedDuplicateEvent(Guid ReportId, Guid ReporterId, Guid PrimaryReportId) : IDomainEvent;

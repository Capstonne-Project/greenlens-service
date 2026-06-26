using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>Raised when a report is verified by LEO (Submitted → Verified).</summary>
public sealed record ReportVerifiedEvent(Guid ReportId, Guid ReporterId) : IDomainEvent;

/// <summary>Raised when a report is rejected by LEO (Submitted → Rejected).</summary>
public sealed record ReportRejectedEvent(Guid ReportId, Guid ReporterId) : IDomainEvent;

/// <summary>Raised when a report is resolved (InProgress → Resolved).</summary>
public sealed record ReportResolvedEvent(Guid ReportId, Guid ReporterId) : IDomainEvent;

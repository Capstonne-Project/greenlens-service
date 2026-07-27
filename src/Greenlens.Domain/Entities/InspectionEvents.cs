using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>Raised when an inspection penalty decision is issued (BR-INS-012, BR-NTF-002).</summary>
public sealed record PenaltyIssuedEvent(
    Guid InspectionReportId,
    Guid ReportId,
    decimal PenaltyAmount,
    string DecisionNumber,
    ViolationLevel ViolationLevel) : IDomainEvent;

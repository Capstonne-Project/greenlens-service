using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Inspection.GetInspectionReportById;

/// <summary>Get full details of an InspectionReport.</summary>
public sealed record GetInspectionReportByIdQuery(Guid InspectionId) : IRequest<Result<InspectionReportDetailResponse>>;

public sealed record InspectionReportDetailResponse(
    Guid Id,
    Guid ReportId,
    string ReportCode,
    InspectionStatus Status,
    // Team
    Guid? AssignedTeamId,
    string? AssignedTeamName,
    // Violation
    string? ViolationDescription,
    string? ViolatorName,
    string? ViolatorAddress,
    string? ViolatorIdentity,
    ViolationLevel? ViolationLevel,
    // Penalty
    decimal? PenaltyAmount,
    string? PenaltyDecisionNumber,
    DateTime? PenaltyIssuedAt,
    DateTime? PenaltyDueDate,
    decimal? PaidAmount,
    string? AdditionalPenaltyMeasures,
    bool IsRepeatOffender,
    // Officers
    Guid CreatedByOfficerId,
    string? CreatedByOfficerName,
    Guid? IssuedByInspectorId,
    string? IssuedByInspectorName,
    // Lifecycle
    DateTime? SlaInspectionDueAt,
    DateTime? ClosedAt,
    string? ClosedReason,
    DateTime CreatedAt);

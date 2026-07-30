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
    // ViolatingEntity (linked)
    Guid? ViolatingEntityId,
    ViolatingEntityEmbeddedDto? ViolatingEntity,
    // Payment history
    List<PenaltyPaymentDto> Payments,
    // Checklist workflow (BR-INS-033)
    DateTime? AcceptedAt,
    Guid? AcceptedByUserId,
    DateTime? ArrivalConfirmedAt,
    decimal? ArrivalLatitude,
    decimal? ArrivalLongitude,
    string? ArrivalNote,
    DateTime? FieldInvestigationSubmittedAt,
    Guid? FieldInvestigationSubmittedByUserId,
    IReadOnlyList<InspectionEvidenceItemDto> ChecklistEvidence,
    // Officers
    Guid CreatedByOfficerId,
    string? CreatedByOfficerName,
    Guid? IssuedByInspectorId,
    string? IssuedByInspectorName,
    // Lifecycle
    DateTime? SlaInspectionDueAt,
    DateTime? ClosedAt,
    string? ClosedReason,
    DateTime CreatedAt,
    // UI capability flags
    bool CanAcceptTask,
    bool CanConfirmArrival,
    bool CanEditChecklist,
    bool CanSubmitFieldReport,
    bool CanEditDetails,
    bool CanIssuePenalty,
    bool CanCloseNoViolation,
    bool CanRecordPayment,
    bool CanClose);

/// <summary>Single checklist evidence item.</summary>
public sealed record InspectionEvidenceItemDto(
    Guid Id,
    InspectionEvidenceCategory Category,
    string? MediaUrl,
    string? MimeType,
    long? SizeBytes,
    string? Description,
    int? DurationSeconds,
    DateTime UploadedAt);

/// <summary>Embedded violating entity info within inspection detail.</summary>
public sealed record ViolatingEntityEmbeddedDto(
    Guid Id,
    string Name,
    ViolatorType Type,
    string? Address,
    string? TaxCode,
    string? IdentityNumber,
    string? PhoneNumber);

/// <summary>Single payment record — BR-INS-020.</summary>
public sealed record PenaltyPaymentDto(
    Guid Id,
    decimal Amount,
    DateTime PaidAt,
    string? EvidenceUrl,
    string? Note,
    Guid RecordedByUserId,
    string? RecordedByUserName,
    DateTime CreatedAt);

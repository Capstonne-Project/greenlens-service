using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Inspection.GetInspectionsByReport;

/// <summary>Get all inspection reports linked to a report.</summary>
public sealed record GetInspectionsByReportQuery(Guid ReportId) : IRequest<Result<GetInspectionsByReportResponse>>;

public sealed record GetInspectionsByReportResponse(List<InspectionSummaryDto> Items);

public sealed record InspectionSummaryDto(
    Guid Id,
    InspectionStatus Status,
    string? ViolatorName,
    ViolationLevel? ViolationLevel,
    decimal? PenaltyAmount,
    decimal? PaidAmount,
    bool IsRepeatOffender,
    Guid CreatedByOfficerId,
    string? CreatedByOfficerName,
    DateTime? SlaInspectionDueAt,
    DateTime? ClosedAt,
    DateTime CreatedAt);

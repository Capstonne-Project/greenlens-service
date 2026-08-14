using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetViolationRecurrenceComparison;

/// <summary>Side-by-side comparison for LEO when BR-REP-034 flag is set.</summary>
public sealed record GetViolationRecurrenceComparisonQuery(Guid ReportId)
    : IRequest<Result<ViolationRecurrenceComparisonResponse>>;

public sealed record ViolationRecurrenceComparisonResponse(
    ViolationRecurrenceReportSide CurrentReport,
    ViolationRecurrenceReportSide PriorClosedReport,
    int DaysSincePriorClosed,
    double DistanceMeters);

public sealed record ViolationRecurrenceReportSide(
    Guid Id,
    string Code,
    ReportStatus Status,
    string CategoryCode,
    string CategoryName,
    Severity Severity,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    IReadOnlyList<ViolationRecurrenceMediaItem> Media,
    bool HadPriorInspection,
    Guid? PriorInspectionId,
    string? PriorInspectionFinalStatus,
    bool HasInspection);

public sealed record ViolationRecurrenceMediaItem(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    MediaType Type,
    DateTime UploadedAt);

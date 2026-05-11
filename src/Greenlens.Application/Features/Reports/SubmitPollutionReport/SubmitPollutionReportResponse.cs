using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>Category snapshot returned after submit (for UI labels).</summary>
public sealed record SubmitPollutionReportCategoryInfo(
    Guid Id,
    string Code,
    string NameVi,
    string NameEn,
    string? IconUrl);

/// <summary>One persisted image row linked to the new report.</summary>
public sealed record SubmitPollutionReportImageInfo(
    Guid Id,
    string Url,
    string MimeType,
    long SizeBytes);

/// <summary>Payload returned when a pollution report is created successfully.</summary>
public sealed record SubmitPollutionReportResponse(
    Guid Id,
    string Code,
    SubmitPollutionReportCategoryInfo Category,
    Severity Severity,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? WardCode,
    string? ProvinceCode,
    bool IsAnonymous,
    Guid? ReporterId,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime? SlaVerifyDueAt,
    bool AiPending,
    IReadOnlyList<SubmitPollutionReportImageInfo> Images);

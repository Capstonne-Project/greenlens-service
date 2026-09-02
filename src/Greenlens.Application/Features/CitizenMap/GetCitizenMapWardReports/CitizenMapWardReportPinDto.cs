using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWardReports;

/// <summary>Single report pin within a ward — preview fields for the small popup card, full detail fetched separately on demand.</summary>
public sealed record CitizenMapWardReportPinDto(
    Guid Id,
    string Code,
    decimal Latitude,
    decimal Longitude,
    Severity Severity,
    string CategoryCode,
    /// <summary>Category display name (Vietnamese) for the card title.</summary>
    string Title,
    string? CategoryIconUrl,
    string? Description,
    string? Address,
    int ReporterCount,
    /// <summary>First report image URL (thumbnail when available).</summary>
    string? ImageUrl,
    ReportStatus Status,
    DateTime CreatedAt);

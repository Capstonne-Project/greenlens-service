using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Map.GetPublicMapReports;

/// <summary>
/// Single report pin for public map (coordinates rounded per BR-MAP-004).
/// Includes preview fields for the map callout card (image, title, address, reporter count).
/// </summary>
public sealed record PublicMapReportPinDto(
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
    DateTime CreatedAt,
    /// <summary>True when the report has an active (not Completed/Cancelled) Community Cleanup program — renders as a distinct "Cộng đồng" marker on the map.</summary>
    bool HasActiveCommunityCleanup,
    /// <summary>Id of the active Community Cleanup event, when <see cref="HasActiveCommunityCleanup"/> is true — used to deep-link into the program detail.</summary>
    Guid? CommunityCleanupEventId);

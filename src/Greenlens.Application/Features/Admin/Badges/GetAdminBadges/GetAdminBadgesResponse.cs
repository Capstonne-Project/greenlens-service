using Greenlens.Application.Common.Models;

namespace Greenlens.Application.Features.Admin.Badges.GetAdminBadges;

/// <summary>Response containing paged badges for Admin Dashboard.</summary>
public sealed record GetAdminBadgesResponse(
    IReadOnlyList<AdminBadgeItem> Items,
    PaginationMeta Pagination);

/// <summary>Single badge row for admin management table.</summary>
public sealed record AdminBadgeItem(
    Guid Id,
    string Code,
    string NameVi,
    string NameEn,
    string? Description,
    string? IconUrl,
    bool IsActive,
    int? RequiredPoints,
    int? RequiredReportCount,
    int? RequiredStreakDays,
    int? RequiredActionCount,
    DateTime CreatedAt);

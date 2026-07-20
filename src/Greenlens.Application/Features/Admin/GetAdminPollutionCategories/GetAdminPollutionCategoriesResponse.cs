using Greenlens.Application.Common.Models;

namespace Greenlens.Application.Features.Admin.GetAdminPollutionCategories;

/// <summary>Response containing paged pollution categories for Admin Dashboard.</summary>
public sealed record GetAdminPollutionCategoriesResponse(
    IReadOnlyList<AdminPollutionCategoryItem> Items,
    PaginationMeta Pagination);

/// <summary>Single pollution category row for admin management table.</summary>
public sealed record AdminPollutionCategoryItem(
    Guid Id,
    string Code,
    string NameVi,
    string NameEn,
    string? IconUrl,
    bool IsActive,
    int ReportCount,
    DateTime CreatedAt);

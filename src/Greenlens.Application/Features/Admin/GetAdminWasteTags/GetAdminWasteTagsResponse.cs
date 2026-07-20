using Greenlens.Application.Common.Models;

namespace Greenlens.Application.Features.Admin.GetAdminWasteTags;

/// <summary>Response containing paged waste tags for Admin Dashboard.</summary>
public sealed record GetAdminWasteTagsResponse(
    IReadOnlyList<AdminWasteTagItem> Items,
    PaginationMeta Pagination);

/// <summary>Single waste tag row for admin management table.</summary>
public sealed record AdminWasteTagItem(
    Guid Id,
    string Code,
    string NameVi,
    string NameEn,
    string? IconUrl,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    int ReportCount,
    DateTime CreatedAt);

namespace Greenlens.Application.Features.Catalog.GetPollutionCategories;

/// <summary>One pollution category for report form dropdowns.</summary>
public sealed record PollutionCategoryListItemDto(
    Guid Id,
    string Code,
    string NameVi,
    string NameEn,
    string? IconUrl);

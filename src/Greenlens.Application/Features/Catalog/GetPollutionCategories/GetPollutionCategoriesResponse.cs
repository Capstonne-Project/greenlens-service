namespace Greenlens.Application.Features.Catalog.GetPollutionCategories;

public sealed record GetPollutionCategoriesResponse(IReadOnlyList<PollutionCategoryListItemDto> Items);

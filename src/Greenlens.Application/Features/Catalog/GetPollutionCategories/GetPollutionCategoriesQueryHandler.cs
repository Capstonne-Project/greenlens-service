using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Catalog.GetPollutionCategories;

/// <summary>
/// Returns active pollution categories for citizen report forms.
/// </summary>
/// <remarks>
/// Implements: BR-REP-005 (category required on submit — client loads options from this catalog).
/// </remarks>
public sealed class GetPollutionCategoriesQueryHandler(IPollutionCategoryRepository categories)
    : IRequestHandler<GetPollutionCategoriesQuery, Result<GetPollutionCategoriesResponse>>
{
    public async Task<Result<GetPollutionCategoriesResponse>> Handle(
        GetPollutionCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await categories.QueryAsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Code)
            .Select(c => new PollutionCategoryListItemDto(
                c.Id,
                c.Code,
                c.NameVi,
                c.NameEn,
                c.IconUrl))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GetPollutionCategoriesResponse(items);
    }
}

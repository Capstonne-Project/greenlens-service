using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Catalog.GetPollutionCategories;

/// <summary>Active pollution categories for report submission (BR-REP-005).</summary>
public sealed record GetPollutionCategoriesQuery : IRequest<Result<GetPollutionCategoriesResponse>>;

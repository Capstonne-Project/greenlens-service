using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.GetAdminPollutionCategories;

/// <summary>
/// Returns all pollution categories (including inactive) for Admin Dashboard.
/// Supports search, filter by isActive, sort, and pagination.
/// </summary>
/// <remarks>Implements: BR-ADM-003 (CRUD Category management).</remarks>
public sealed record GetAdminPollutionCategoriesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetAdminPollutionCategoriesResponse>>;

using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.GetAdminPollutionCategories;

/// <summary>
/// Handles the GetAdminPollutionCategoriesQuery — returns all pollution categories with
/// search, filter (isActive), sort, and pagination for Admin Dashboard.
/// </summary>
/// <remarks>Implements: BR-ADM-003 (CRUD Category management).</remarks>
public sealed class GetAdminPollutionCategoriesQueryHandler(IPollutionCategoryRepository categories, ILogger<GetAdminPollutionCategoriesQueryHandler> logger)
    : IRequestHandler<GetAdminPollutionCategoriesQuery, Result<GetAdminPollutionCategoriesResponse>>
{
    public async Task<Result<GetAdminPollutionCategoriesResponse>> Handle(
        GetAdminPollutionCategoriesQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting admin pollution categories");

        // 1. Base query — include inactive categories for admin
        var baseQuery = categories.QueryAsNoTracking();

        // 2. Apply search (code, nameVi, nameEn)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            logger.LogInformation("Search: {Search}", request.Search);
            var keyword = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(c =>
                c.Code.ToLower().Contains(keyword) ||
                c.NameVi.ToLower().Contains(keyword) ||
                c.NameEn.ToLower().Contains(keyword));
        }

        // 3. Apply isActive filter
        if (request.IsActive.HasValue)
        {
            baseQuery = baseQuery.Where(c => c.IsActive == request.IsActive.Value);
            logger.LogInformation("Is active: {IsActive}", request.IsActive.Value);
        }

        // 4. Count total
        var totalItems = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total items: {TotalItems}", totalItems);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalItems);

        // 5. Apply sorting
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        logger.LogInformation("Sort by: {SortBy}", sortBy);
        var orderedQuery = sortBy switch
        {
            "code" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.Code)
                : baseQuery.OrderBy(c => c.Code),
            "namevi" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.NameVi)
                : baseQuery.OrderBy(c => c.NameVi),
            "nameen" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.NameEn)
                : baseQuery.OrderBy(c => c.NameEn),
            "isactive" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.IsActive)
                : baseQuery.OrderBy(c => c.IsActive),
            "reportcount" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.Reports.Count)
                : baseQuery.OrderBy(c => c.Reports.Count),
            "createdat" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.CreatedAt)
                : baseQuery.OrderBy(c => c.CreatedAt),
            _ => baseQuery.OrderBy(c => c.Code) // default sort by code ASC
        };

        // 6. Paginate & project
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new AdminPollutionCategoryItem(
                c.Id,
                c.Code,
                c.NameVi,
                c.NameEn,
                c.IconUrl,
                c.IsActive,
                c.Reports.Count,
                c.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Admin pollution categories retrieved successfully");

        return new GetAdminPollutionCategoriesResponse(items, pagination);
    }
}

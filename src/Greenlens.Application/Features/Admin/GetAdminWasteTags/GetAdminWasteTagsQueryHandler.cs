using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.GetAdminWasteTags;

/// <summary>
/// Handles the GetAdminWasteTagsQuery — returns all waste tags with
/// search, filter (isActive), sort, and pagination for Admin Dashboard.
/// </summary>
public sealed class GetAdminWasteTagsQueryHandler(IWasteTagRepository wasteTags, ILogger<GetAdminWasteTagsQueryHandler> logger)
    : IRequestHandler<GetAdminWasteTagsQuery, Result<GetAdminWasteTagsResponse>>
{
    public async Task<Result<GetAdminWasteTagsResponse>> Handle(
        GetAdminWasteTagsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting admin waste tags");

        // 1. Base query — include inactive tags for admin
        var baseQuery = wasteTags.QueryAsNoTracking();

        // 2. Apply search (code, nameVi, nameEn, description)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            logger.LogInformation("Search: {Search}", request.Search);
            var keyword = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(t =>
                t.Code.ToLower().Contains(keyword) ||
                t.NameVi.ToLower().Contains(keyword) ||
                t.NameEn.ToLower().Contains(keyword) ||
                (t.Description != null && t.Description.ToLower().Contains(keyword)));
        }

        // 3. Apply isActive filter
        if (request.IsActive.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.IsActive == request.IsActive.Value);
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
                ? baseQuery.OrderByDescending(t => t.Code)
                : baseQuery.OrderBy(t => t.Code),
            "namevi" => request.SortDesc
                ? baseQuery.OrderByDescending(t => t.NameVi)
                : baseQuery.OrderBy(t => t.NameVi),
            "nameen" => request.SortDesc
                ? baseQuery.OrderByDescending(t => t.NameEn)
                : baseQuery.OrderBy(t => t.NameEn),
            "isactive" => request.SortDesc
                ? baseQuery.OrderByDescending(t => t.IsActive)
                : baseQuery.OrderBy(t => t.IsActive),
            "reportcount" => request.SortDesc
                ? baseQuery.OrderByDescending(t => t.ReportWasteTags.Count)
                : baseQuery.OrderBy(t => t.ReportWasteTags.Count),
            "createdat" => request.SortDesc
                ? baseQuery.OrderByDescending(t => t.CreatedAt)
                : baseQuery.OrderBy(t => t.CreatedAt),
            _ => baseQuery.OrderBy(t => t.DisplayOrder) // default sort by displayOrder ASC
        };

        // 6. Paginate & project
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new AdminWasteTagItem(
                t.Id,
                t.Code,
                t.NameVi,
                t.NameEn,
                t.IconUrl,
                t.Description,
                t.DisplayOrder,
                t.IsActive,
                t.ReportWasteTags.Count,
                t.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Admin waste tags retrieved successfully");

        return new GetAdminWasteTagsResponse(items, pagination);
    }
}

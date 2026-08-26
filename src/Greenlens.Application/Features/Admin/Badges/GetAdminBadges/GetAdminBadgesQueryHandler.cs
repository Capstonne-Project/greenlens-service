using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.Badges.GetAdminBadges;

/// <summary>
/// Returns all badges with search, filter (isActive), sort, and pagination for Admin Dashboard.
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed class GetAdminBadgesQueryHandler(
    IBadgeRepository badges,
    ILogger<GetAdminBadgesQueryHandler> logger)
    : IRequestHandler<GetAdminBadgesQuery, Result<GetAdminBadgesResponse>>
{
    public async Task<Result<GetAdminBadgesResponse>> Handle(
        GetAdminBadgesQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting admin badges");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var baseQuery = badges.QueryAsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLowerInvariant();
            logger.LogInformation("Search: {Search}", request.Search);
            baseQuery = baseQuery.Where(b =>
                b.Code.ToLower().Contains(keyword) ||
                b.NameVi.ToLower().Contains(keyword) ||
                b.NameEn.ToLower().Contains(keyword) ||
                (b.Description != null && b.Description.ToLower().Contains(keyword)));
        }

        if (request.IsActive.HasValue)
        {
            logger.LogInformation("Is active: {IsActive}", request.IsActive.Value);
            baseQuery = baseQuery.Where(b => b.IsActive == request.IsActive.Value);
        }

        var totalItems = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total items: {TotalItems}", totalItems);
        var pagination = PaginationMeta.Create(page, pageSize, totalItems);

        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        logger.LogInformation("Sort by: {SortBy}", sortBy);
        var orderedQuery = sortBy switch
        {
            "code" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.Code)
                : baseQuery.OrderBy(b => b.Code),
            "namevi" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.NameVi)
                : baseQuery.OrderBy(b => b.NameVi),
            "nameen" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.NameEn)
                : baseQuery.OrderBy(b => b.NameEn),
            "isactive" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.IsActive)
                : baseQuery.OrderBy(b => b.IsActive),
            "requiredpoints" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.RequiredPoints)
                : baseQuery.OrderBy(b => b.RequiredPoints),
            "requiredreportcount" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.RequiredReportCount)
                : baseQuery.OrderBy(b => b.RequiredReportCount),
            "requiredstreakdays" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.RequiredStreakDays)
                : baseQuery.OrderBy(b => b.RequiredStreakDays),
            "requiredactioncount" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.RequiredActionCount)
                : baseQuery.OrderBy(b => b.RequiredActionCount),
            "createdat" => request.SortDesc
                ? baseQuery.OrderByDescending(b => b.CreatedAt)
                : baseQuery.OrderBy(b => b.CreatedAt),
            _ => baseQuery.OrderBy(b => b.Code)
        };

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new AdminBadgeItem(
                b.Id,
                b.Code,
                b.NameVi,
                b.NameEn,
                b.Description,
                b.IconUrl,
                b.IsActive,
                b.RequiredPoints,
                b.RequiredReportCount,
                b.RequiredStreakDays,
                b.RequiredActionCount,
                b.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Admin badges retrieved successfully");

        return new GetAdminBadgesResponse(items, pagination);
    }
}

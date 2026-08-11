using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.GamificationConfigs.GetGamificationConfigs;

/// <summary>
/// Returns gamification configs with search, filter (isActive), sort, and pagination.
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed class GetGamificationConfigsQueryHandler(
    IApplicationDbContext db,
    ILogger<GetGamificationConfigsQueryHandler> logger)
    : IRequestHandler<GetGamificationConfigsQuery, Result<GetGamificationConfigsResponse>>
{
    public async Task<Result<GetGamificationConfigsResponse>> Handle(
        GetGamificationConfigsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting gamification configs");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var baseQuery = db.Set<GamificationConfig>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLowerInvariant();
            logger.LogInformation("Search: {Search}", request.Search);
            baseQuery = baseQuery.Where(c =>
                c.ActionType.ToString().ToLower().Contains(keyword) ||
                c.Description.ToLower().Contains(keyword));
        }

        if (request.IsActive.HasValue)
        {
            logger.LogInformation("Is active: {IsActive}", request.IsActive.Value);
            baseQuery = baseQuery.Where(c => c.IsActive == request.IsActive.Value);
        }

        var totalItems = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total items: {TotalItems}", totalItems);
        var pagination = PaginationMeta.Create(page, pageSize, totalItems);

        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        logger.LogInformation("Sort by: {SortBy}", sortBy);
        var orderedQuery = sortBy switch
        {
            "actiontype" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.ActionType)
                : baseQuery.OrderBy(c => c.ActionType),
            "points" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.Points)
                : baseQuery.OrderBy(c => c.Points),
            "isactive" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.IsActive)
                : baseQuery.OrderBy(c => c.IsActive),
            "createdat" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.CreatedAt)
                : baseQuery.OrderBy(c => c.CreatedAt),
            "updatedat" => request.SortDesc
                ? baseQuery.OrderByDescending(c => c.UpdatedAt)
                : baseQuery.OrderBy(c => c.UpdatedAt),
            _ => baseQuery.OrderBy(c => c.ActionType)
        };

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new GamificationConfigItem(
                c.Id,
                c.ActionType.ToString(),
                c.Points,
                c.Description,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Gamification configs retrieved successfully");

        return new GetGamificationConfigsResponse(items, pagination);
    }
}

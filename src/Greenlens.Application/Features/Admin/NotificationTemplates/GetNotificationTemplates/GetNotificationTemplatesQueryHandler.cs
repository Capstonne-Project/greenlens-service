using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplates;

/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class GetNotificationTemplatesQueryHandler(
    IApplicationDbContext db,
    ILogger<GetNotificationTemplatesQueryHandler> logger)
    : IRequestHandler<GetNotificationTemplatesQuery, Result<GetNotificationTemplatesResponse>>
{
    public async Task<Result<GetNotificationTemplatesResponse>> Handle(
        GetNotificationTemplatesQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting notification templates");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.Set<NotificationTemplate>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLowerInvariant();
            logger.LogInformation("Search: {Search}", request.Search);
            query = query.Where(t =>
                t.TemplateKey.ToLower().Contains(keyword) ||
                t.TitleVi.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(request.Channel)
            && Enum.TryParse<NotificationChannel>(request.Channel, true, out var channel))
        {
            logger.LogInformation("Channel: {Channel}", request.Channel);
            query = query.Where(t => t.Channel == channel);
        }

        if (request.IsPublished.HasValue)
        {
            logger.LogInformation("Is published: {IsPublished}", request.IsPublished.Value);
            query = query.Where(t => t.IsPublished == request.IsPublished.Value);
        }

        if (request.IsActive.HasValue)
        {
            logger.LogInformation("Is active: {IsActive}", request.IsActive.Value);
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total count: {TotalCount}", totalCount);

        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        logger.LogInformation("Sort by: {SortBy}", sortBy);
        var orderedQuery = sortBy switch
        {
            "templatekey" => request.SortDesc
                ? query.OrderByDescending(t => t.TemplateKey)
                : query.OrderBy(t => t.TemplateKey),
            "titlevi" => request.SortDesc
                ? query.OrderByDescending(t => t.TitleVi)
                : query.OrderBy(t => t.TitleVi),
            "channel" => request.SortDesc
                ? query.OrderByDescending(t => t.Channel)
                : query.OrderBy(t => t.Channel),
            "type" => request.SortDesc
                ? query.OrderByDescending(t => t.Type)
                : query.OrderBy(t => t.Type),
            "ispublished" => request.SortDesc
                ? query.OrderByDescending(t => t.IsPublished)
                : query.OrderBy(t => t.IsPublished),
            "isactive" => request.SortDesc
                ? query.OrderByDescending(t => t.IsActive)
                : query.OrderBy(t => t.IsActive),
            "updatedat" => request.SortDesc
                ? query.OrderByDescending(t => t.UpdatedAt)
                : query.OrderBy(t => t.UpdatedAt),
            "createdat" => request.SortDesc
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new NotificationTemplateItem(
                t.Id, t.TemplateKey, t.TitleVi,
                t.Channel.ToString(), t.Type.ToString(),
                t.IsPublished, t.IsActive,
                t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Notification templates retrieved successfully");

        return new GetNotificationTemplatesResponse(items, totalCount);
    }
}

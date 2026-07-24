using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplates;

/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class GetNotificationTemplatesQueryHandler(IApplicationDbContext db, ILogger<GetNotificationTemplatesQueryHandler> logger)
    : IRequestHandler<GetNotificationTemplatesQuery, Result<GetNotificationTemplatesResponse>>
{
    public async Task<Result<GetNotificationTemplatesResponse>> Handle(
        GetNotificationTemplatesQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting notification templates");

        var query = db.Set<NotificationTemplate>()
            .AsNoTracking()
            .AsQueryable();

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

        query = query.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total count: {TotalCount}", totalCount);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

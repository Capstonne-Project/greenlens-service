using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplates;

/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class GetNotificationTemplatesQueryHandler(DbContext db)
    : IRequestHandler<GetNotificationTemplatesQuery, Result<GetNotificationTemplatesResponse>>
{
    public async Task<Result<GetNotificationTemplatesResponse>> Handle(
        GetNotificationTemplatesQuery request,
        CancellationToken ct)
    {
        var query = db.Set<NotificationTemplate>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Channel)
            && Enum.TryParse<NotificationChannel>(request.Channel, true, out var channel))
            query = query.Where(t => t.Channel == channel);

        if (request.IsPublished.HasValue)
            query = query.Where(t => t.IsPublished == request.IsPublished.Value);

        query = query.OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

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

        return new GetNotificationTemplatesResponse(items, totalCount);
    }
}

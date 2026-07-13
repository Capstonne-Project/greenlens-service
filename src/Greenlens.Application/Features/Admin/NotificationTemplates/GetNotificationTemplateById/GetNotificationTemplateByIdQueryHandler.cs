using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplateById;

/// <summary>
/// Retrieves a single notification template by ID.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class GetNotificationTemplateByIdQueryHandler(
    IApplicationDbContext db)
    : IRequestHandler<GetNotificationTemplateByIdQuery, Result<NotificationTemplateDetailResponse>>
{
    public async Task<Result<NotificationTemplateDetailResponse>> Handle(
        GetNotificationTemplateByIdQuery request, CancellationToken ct)
    {
        var template = await db.Set<NotificationTemplate>()
            .AsNoTracking()
            .ProjectToType<NotificationTemplateDetailResponse>()
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (template is null)
            return Result<NotificationTemplateDetailResponse>.Failure(
                new Error("NotificationTemplate.NotFound", "Template không tồn tại.", ErrorType.NotFound));

        return template;
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplateById;

/// <summary>
/// Retrieves a single notification template by ID.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class GetNotificationTemplateByIdQueryHandler(
    IApplicationDbContext db,
    ILogger<GetNotificationTemplateByIdQueryHandler> logger)
    : IRequestHandler<GetNotificationTemplateByIdQuery, Result<NotificationTemplateDetailResponse>>
{
    public async Task<Result<NotificationTemplateDetailResponse>> Handle(
        GetNotificationTemplateByIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting notification template by id: {Id}", request.Id);

        // Filter on entity first, then project — ProjectToType then Where is not EF-translatable
        var template = await db.Set<NotificationTemplate>()
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new NotificationTemplateDetailResponse(
                t.Id,
                t.TemplateKey,
                t.TitleVi,
                t.BodyVi,
                t.TitleEn,
                t.BodyEn,
                t.Channel,
                t.Type,
                t.IsPublished,
                t.IsActive,
                t.CreatedAt,
                t.UpdatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (template is null)
        {
            logger.LogWarning("Notification template not found: {Id}", request.Id);
            return Result<NotificationTemplateDetailResponse>.Failure(Errors.Admin.NotificationTemplateNotFound);
        }

        logger.LogInformation("Notification template retrieved successfully: {Id}", request.Id);

        return template;
    }
}

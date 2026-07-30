using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.NotificationTemplates.CreateNotificationTemplate;

/// <summary>
/// Creates a new notification template in draft state (not published).
/// </summary>
/// <remarks>Implements: BR-ADM-004, BR-ADM-010.</remarks>
public sealed class CreateNotificationTemplateCommandHandler(
    INotificationTemplateRepository templates,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<CreateNotificationTemplateCommandHandler> logger)
    : IRequestHandler<CreateNotificationTemplateCommand, Result<CreateNotificationTemplateResponse>>
{
    public async Task<Result<CreateNotificationTemplateResponse>> Handle(
        CreateNotificationTemplateCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Creating notification template");

        var duplicate = await templates
            .ExistsAsync(t => t.TemplateKey == request.TemplateKey && t.Channel == request.Channel, ct)
            .ConfigureAwait(false);

        if (duplicate)
        {
            logger.LogWarning("Notification template already exists: {TemplateKey} {Channel}", request.TemplateKey, request.Channel);
            return Result<CreateNotificationTemplateResponse>.Failure(
                Errors.Admin.NotificationTemplateDuplicate(request.TemplateKey, request.Channel.ToString()));
        }
        var entity = NotificationTemplate.Create(
            request.TemplateKey, request.TitleVi, request.BodyVi,
            request.TitleEn, request.BodyEn, request.Channel, request.Type);

        templates.Add(entity);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Notification template created successfully: {TemplateKey} {Channel}", request.TemplateKey, request.Channel);

        await auditLogger.LogAsync(
            "CreateNotificationTemplate",
            "NotificationTemplate",
            entity.Id.ToString(),
            oldValues: null,
            newValues: JsonSerializer.Serialize(new
            {
                entity.TemplateKey,
                entity.Channel,
                entity.Type,
                entity.IsPublished
            }),
            ct).ConfigureAwait(false);

        return new CreateNotificationTemplateResponse(entity.Id, entity.TemplateKey, entity.IsPublished);
    }
}

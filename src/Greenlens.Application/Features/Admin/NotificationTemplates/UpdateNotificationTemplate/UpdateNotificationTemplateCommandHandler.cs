using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.UpdateNotificationTemplate;

/// <summary>
/// Handles updating a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004, BR-ADM-010.</remarks>
public sealed class UpdateNotificationTemplateCommandHandler(
    IApplicationDbContext db,
    IAuditLogger auditLogger,
    ILogger<UpdateNotificationTemplateCommandHandler> logger)
    : IRequestHandler<UpdateNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(UpdateNotificationTemplateCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating notification template");

        var template = await db.Set<Greenlens.Domain.Entities.NotificationTemplate>().FindAsync([request.Id], ct);

        if (template is null)
        {
            logger.LogWarning("Notification template not found: {Id}", request.Id);
            return Result.Failure(Errors.Admin.NotificationTemplateNotFound);
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            template.TitleVi,
            template.IsPublished
        });

        template.Update(
            request.TitleVi,
            request.BodyVi,
            request.TitleEn,
            request.BodyEn);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdateNotificationTemplate",
            "NotificationTemplate",
            template.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                template.TitleVi,
                template.IsPublished
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Notification template updated successfully: {Id}", request.Id);

        return Result.Success();
    }
}

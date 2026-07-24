using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.NotificationTemplates.UpdateNotificationTemplate;

/// <summary>
/// Handles updating a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class UpdateNotificationTemplateCommandHandler(
    IApplicationDbContext db,
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

        template.Update(
            request.TitleVi,
            request.BodyVi,
            request.TitleEn,
            request.BodyEn);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Notification template updated successfully: {Id}", request.Id);

        return Result.Success();
    }
}

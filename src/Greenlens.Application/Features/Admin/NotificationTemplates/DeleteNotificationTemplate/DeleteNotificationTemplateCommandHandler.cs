using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.NotificationTemplates.DeleteNotificationTemplate;

/// <summary>
/// Handles deactivating a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class DeleteNotificationTemplateCommandHandler(
    IApplicationDbContext db,
    ILogger<DeleteNotificationTemplateCommandHandler> logger)
    : IRequestHandler<DeleteNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(DeleteNotificationTemplateCommand request, CancellationToken ct)
    {
        logger.LogInformation("Deleting notification template");

        var template = await db.Set<Greenlens.Domain.Entities.NotificationTemplate>().FindAsync([request.Id], ct);
        
        if (template is null)
        {
            logger.LogWarning("Notification template not found: {Id}", request.Id);
            return Result.Failure(Errors.Admin.NotificationTemplateNotFound);
        }

        template.Deactivate();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Notification template deleted successfully: {Id}", request.Id);

        return Result.Success();
    }
}

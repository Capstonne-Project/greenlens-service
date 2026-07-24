using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.NotificationTemplates.PublishNotificationTemplate;

/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class PublishNotificationTemplateCommandHandler(
    INotificationTemplateRepository templates,
    IUnitOfWork uow,
    ILogger<PublishNotificationTemplateCommandHandler> logger)
    : IRequestHandler<PublishNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(PublishNotificationTemplateCommand request, CancellationToken ct)
    {
        logger.LogInformation("Publishing notification template");

        var template = await templates.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (template is null)
        {
            logger.LogWarning("Notification template not found: {Id}", request.Id);
            return Result.Failure(Errors.Admin.NotificationTemplateNotFound);
        }
        if (request.Publish)
        {
            logger.LogInformation("Publishing notification template: {Id}", request.Id);
            template.Publish();
        }
        else
        {
            logger.LogInformation("Unpublishing notification template: {Id}", request.Id);
            template.Unpublish();
        }
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Notification template published successfully: {Id}", request.Id);
        return Result.Success();
    }
}

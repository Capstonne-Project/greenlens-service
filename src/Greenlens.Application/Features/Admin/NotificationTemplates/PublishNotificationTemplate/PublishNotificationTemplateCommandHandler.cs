using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.PublishNotificationTemplate;

/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class PublishNotificationTemplateCommandHandler(
    INotificationTemplateRepository templates,
    IUnitOfWork uow)
    : IRequestHandler<PublishNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(PublishNotificationTemplateCommand request, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (template is null)
            return Result.Failure(Errors.Admin.NotificationTemplateNotFound);

        if (request.Publish)
            template.Publish();
        else
            template.Unpublish();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

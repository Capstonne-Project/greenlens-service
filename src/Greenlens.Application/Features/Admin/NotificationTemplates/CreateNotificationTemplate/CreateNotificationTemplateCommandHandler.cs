using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.CreateNotificationTemplate;

/// <summary>
/// Creates a new notification template in draft state (not published).
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class CreateNotificationTemplateCommandHandler(
    INotificationTemplateRepository templates,
    IUnitOfWork uow)
    : IRequestHandler<CreateNotificationTemplateCommand, Result<CreateNotificationTemplateResponse>>
{
    public async Task<Result<CreateNotificationTemplateResponse>> Handle(
        CreateNotificationTemplateCommand request,
        CancellationToken ct)
    {
        var duplicate = await templates
            .ExistsAsync(t => t.TemplateKey == request.TemplateKey && t.Channel == request.Channel, ct)
            .ConfigureAwait(false);

        if (duplicate)
            return Result<CreateNotificationTemplateResponse>.Failure(
                Errors.Admin.NotificationTemplateDuplicate(request.TemplateKey, request.Channel.ToString()));

        var entity = NotificationTemplate.Create(
            request.TemplateKey, request.TitleVi, request.BodyVi,
            request.TitleEn, request.BodyEn, request.Channel, request.Type);

        templates.Add(entity);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return new CreateNotificationTemplateResponse(entity.Id, entity.TemplateKey, entity.IsPublished);
    }
}

using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.PublishNotificationTemplate;

/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class PublishNotificationTemplateCommandHandler(DbContext db)
    : IRequestHandler<PublishNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(PublishNotificationTemplateCommand request, CancellationToken ct)
    {
        var template = await db.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (template is null)
            return Result.Failure(
                new Error("NotificationTemplate.NotFound", "Template không tồn tại.", ErrorType.NotFound));

        if (request.Publish)
            template.Publish();
        else
            template.Unpublish();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

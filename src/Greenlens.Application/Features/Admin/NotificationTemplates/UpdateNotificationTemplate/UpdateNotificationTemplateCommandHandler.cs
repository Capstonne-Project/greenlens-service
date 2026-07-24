using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.UpdateNotificationTemplate;

/// <summary>
/// Handles updating a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class UpdateNotificationTemplateCommandHandler(
    IApplicationDbContext db)
    : IRequestHandler<UpdateNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(UpdateNotificationTemplateCommand request, CancellationToken ct)
    {
        var template = await db.Set<Greenlens.Domain.Entities.NotificationTemplate>().FindAsync([request.Id], ct);
        
        if (template is null)
            return Result.Failure(Errors.Admin.NotificationTemplateNotFound);

        template.Update(
            request.TitleVi,
            request.BodyVi,
            request.TitleEn,
            request.BodyEn);

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

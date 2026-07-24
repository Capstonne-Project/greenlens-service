using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.DeleteNotificationTemplate;

/// <summary>
/// Handles deactivating a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class DeleteNotificationTemplateCommandHandler(
    IApplicationDbContext db)
    : IRequestHandler<DeleteNotificationTemplateCommand, Result>
{
    public async Task<Result> Handle(DeleteNotificationTemplateCommand request, CancellationToken ct)
    {
        var template = await db.Set<Greenlens.Domain.Entities.NotificationTemplate>().FindAsync([request.Id], ct);
        
        if (template is null)
            return Result.Failure(Errors.Admin.NotificationTemplateNotFound);

        template.Deactivate();

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

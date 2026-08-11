using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.Badges.UpdateBadge;

public sealed class UpdateBadgeCommandHandler(
    IBadgeRepository badges,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<UpdateBadgeCommandHandler> logger)
    : IRequestHandler<UpdateBadgeCommand, Result>
{
    public async Task<Result> Handle(UpdateBadgeCommand request, CancellationToken ct)
    {
        var badge = await badges.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (badge is null)
        {
            logger.LogWarning("Badge not found: {Id}", request.Id);
            return Errors.Gamification.BadgeNotFound;
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            badge.NameVi,
            badge.NameEn,
            badge.Description,
            badge.IconUrl,
            badge.IsActive
        });

        badge.Update(request.NameVi, request.NameEn, request.Description, request.IconUrl);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdateBadge",
            "Badge",
            badge.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                badge.NameVi,
                badge.NameEn,
                badge.Description,
                badge.IconUrl,
                badge.IsActive
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Badge {BadgeId} updated", request.Id);
        return Result.Success();
    }
}

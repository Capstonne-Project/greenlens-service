using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.GamificationConfigs.UpdateGamificationConfig;

/// <summary>
/// Updates the point value and description for a gamification config entry.
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed class UpdateGamificationConfigCommandHandler(DbContext db)
    : IRequestHandler<UpdateGamificationConfigCommand, Result>
{
    public async Task<Result> Handle(UpdateGamificationConfigCommand request, CancellationToken ct)
    {
        var config = await db.Set<GamificationConfig>()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (config is null)
            return Result.Failure(
                new Error("GamificationConfig.NotFound", "Cấu hình điểm không tồn tại.", ErrorType.NotFound));

        config.Update(request.Points, request.Description, request.IsActive);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.GamificationConfigs.UpdateGamificationConfig;

/// <summary>
/// Updates the point value and description for a gamification config entry.
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed class UpdateGamificationConfigCommandHandler(
    IGamificationConfigRepository configs,
    IUnitOfWork uow,
    ILogger<UpdateGamificationConfigCommandHandler> logger)
    : IRequestHandler<UpdateGamificationConfigCommand, Result>
{
    public async Task<Result> Handle(UpdateGamificationConfigCommand request, CancellationToken ct)
    {
        var config = await configs.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (config is null)
        {
            logger.LogWarning("Gamification config not found: {Id}", request.Id);
            return Errors.Admin.GamificationConfigNotFound;
        }

        config.Update(request.Points, request.Description, request.IsActive);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Gamification config updated successfully: {Id}", request.Id);

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.DeactivatePenaltyFramework;

/// <summary>
/// Deactivates or reactivates a PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed class DeactivatePenaltyFrameworkCommandHandler(
    IPenaltyFrameworkRepository penaltyFrameworks,
    IUnitOfWork uow,
    ILogger<DeactivatePenaltyFrameworkCommandHandler> logger)
    : IRequestHandler<DeactivatePenaltyFrameworkCommand, Result>
{
    public async Task<Result> Handle(DeactivatePenaltyFrameworkCommand request, CancellationToken ct)
    {
        logger.LogInformation("Deactivating penalty framework");

        var entity = await penaltyFrameworks.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (entity is null)
        {
            logger.LogWarning("Penalty framework not found: {Id}", request.Id);
            return Result.Failure(Errors.Admin.PenaltyFrameworkNotFound);
        }

        if (request.Activate)
        {
            logger.LogInformation("Activating penalty framework: {Id}", request.Id);
            entity.Activate();
        }
        else
        {
            logger.LogInformation("Deactivating penalty framework: {Id}", request.Id);
            entity.Deactivate();
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Penalty framework deactivated successfully: {Id}", request.Id);
        return Result.Success();
    }
}

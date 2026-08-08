using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.UpdatePenaltyFramework;

/// <summary>
/// Updates amounts and effective dates of an existing PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008, BR-ADM-010.</remarks>
public sealed class UpdatePenaltyFrameworkCommandHandler(
    IPenaltyFrameworkRepository penaltyFrameworks,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<UpdatePenaltyFrameworkCommandHandler> logger)
    : IRequestHandler<UpdatePenaltyFrameworkCommand, Result>
{
    public async Task<Result> Handle(UpdatePenaltyFrameworkCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating penalty framework");

        var entity = await penaltyFrameworks.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (entity is null)
        {
            logger.LogWarning("Penalty framework not found: {Id}", request.Id);
            return Result.Failure(Errors.Admin.PenaltyFrameworkNotFound);
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            entity.MinAmount,
            entity.MaxAmount,
            entity.EffectiveFrom,
            entity.EffectiveTo
        });

        entity.Update(request.MinAmount, request.MaxAmount, request.EffectiveFrom, request.EffectiveTo);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdatePenaltyFramework",
            "PenaltyFramework",
            entity.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                entity.MinAmount,
                entity.MaxAmount,
                entity.EffectiveFrom,
                entity.EffectiveTo
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Penalty framework updated successfully: {Id}", request.Id);
        return Result.Success();
    }
}

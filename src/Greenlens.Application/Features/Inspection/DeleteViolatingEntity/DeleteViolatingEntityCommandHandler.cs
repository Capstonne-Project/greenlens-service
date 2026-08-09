using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.DeleteViolatingEntity;

/// <summary>Officer soft-deletes a violating entity record.</summary>
/// <remarks>Implements: BR-INS-022 (entity lifecycle), BR-DAT-002 (retain referential integrity), BR-ADM-010.</remarks>
public sealed class DeleteViolatingEntityCommandHandler(
    IViolatingEntityRepository violatingEntities,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IAuditLogger auditLogger,
    ILogger<DeleteViolatingEntityCommandHandler> logger) : IRequestHandler<DeleteViolatingEntityCommand, Result>
{
    public async Task<Result> Handle(DeleteViolatingEntityCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting delete violating entity");

        var entity = await violatingEntities.GetByIdIncludingDeletedAsync(request.EntityId, ct).ConfigureAwait(false);
        if (entity is null)
        {
            logger.LogWarning("Violating entity not found for entity {EntityId}", request.EntityId);
            return Errors.Inspections.ViolatingEntityNotFound;
        }

        if (entity.IsDeleted)
        {
            logger.LogWarning("Violating entity {EntityId} already deleted", request.EntityId);
            return Errors.Inspections.ViolatingEntityAlreadyDeleted;
        }

        var inUse = await violatingEntities
            .HasAnyInspectionReportsAsync(request.EntityId, ct)
            .ConfigureAwait(false);
        if (inUse)
        {
            logger.LogWarning("Violating entity {EntityId} is in use", request.EntityId);
            return Errors.Inspections.ViolatingEntityInUse;
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            entity.Name,
            entity.TaxCode,
            isDeleted = entity.IsDeleted
        });

        entity.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "DeleteViolatingEntity",
            "ViolatingEntity",
            request.EntityId.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new { isDeleted = true }),
            ct).ConfigureAwait(false);

        logger.LogInformation("ViolatingEntity {EntityId} soft-deleted by {UserId}", request.EntityId, currentUser.UserId);

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.DeleteViolatingEntity;

public sealed class DeleteViolatingEntityCommandHandler(
    IViolatingEntityRepository violatingEntities,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteViolatingEntityCommandHandler> logger) : IRequestHandler<DeleteViolatingEntityCommand, Result>
{
    public async Task<Result> Handle(DeleteViolatingEntityCommand request, CancellationToken ct)
    {
        var entity = await violatingEntities.GetByIdAsync(request.EntityId, ct).ConfigureAwait(false);
        if (entity is null)
            return Errors.Inspections.ViolatingEntityNotFound;

        entity.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("ViolatingEntity {EntityId} soft-deleted by {UserId}", request.EntityId, currentUser.UserId);

        return Result.Success();
    }
}

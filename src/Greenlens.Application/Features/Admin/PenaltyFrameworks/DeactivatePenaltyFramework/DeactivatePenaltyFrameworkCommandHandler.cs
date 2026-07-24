using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.DeactivatePenaltyFramework;

/// <summary>
/// Deactivates or reactivates a PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed class DeactivatePenaltyFrameworkCommandHandler(
    IPenaltyFrameworkRepository penaltyFrameworks,
    IUnitOfWork uow)
    : IRequestHandler<DeactivatePenaltyFrameworkCommand, Result>
{
    public async Task<Result> Handle(DeactivatePenaltyFrameworkCommand request, CancellationToken ct)
    {
        var entity = await penaltyFrameworks.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (entity is null)
            return Result.Failure(Errors.Admin.PenaltyFrameworkNotFound);

        if (request.Activate)
            entity.Activate();
        else
            entity.Deactivate();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

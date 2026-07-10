using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.UpdatePenaltyFramework;

/// <summary>
/// Updates amounts and effective dates of an existing PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008 — does not affect already-issued decisions.</remarks>
public sealed class UpdatePenaltyFrameworkCommandHandler(
    IPenaltyFrameworkRepository penaltyFrameworks,
    IUnitOfWork uow)
    : IRequestHandler<UpdatePenaltyFrameworkCommand, Result>
{
    public async Task<Result> Handle(UpdatePenaltyFrameworkCommand request, CancellationToken ct)
    {
        var entity = await penaltyFrameworks.GetByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (entity is null)
            return Result.Failure(
                new Error("PenaltyFramework.NotFound", "Penalty framework entry not found.", ErrorType.NotFound));

        entity.Update(request.MinAmount, request.MaxAmount, request.EffectiveFrom, request.EffectiveTo);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.UpdatePenaltyFramework;

/// <summary>
/// Updates amounts and effective dates of an existing PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008 — does not affect already-issued decisions.</remarks>
public sealed class UpdatePenaltyFrameworkCommandHandler(DbContext db)
    : IRequestHandler<UpdatePenaltyFrameworkCommand, Result>
{
    public async Task<Result> Handle(UpdatePenaltyFrameworkCommand request, CancellationToken ct)
    {
        var entity = await db.Set<PenaltyFramework>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (entity is null)
            return Result.Failure(
                new Error("PenaltyFramework.NotFound", "Penalty framework entry not found.", ErrorType.NotFound));

        entity.Update(request.MinAmount, request.MaxAmount, request.EffectiveFrom, request.EffectiveTo);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

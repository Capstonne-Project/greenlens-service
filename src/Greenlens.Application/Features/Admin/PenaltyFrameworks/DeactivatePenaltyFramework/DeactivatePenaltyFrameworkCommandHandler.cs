using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.DeactivatePenaltyFramework;

/// <summary>
/// Deactivates or reactivates a PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed class DeactivatePenaltyFrameworkCommandHandler(DbContext db)
    : IRequestHandler<DeactivatePenaltyFrameworkCommand, Result>
{
    public async Task<Result> Handle(DeactivatePenaltyFrameworkCommand request, CancellationToken ct)
    {
        var entity = await db.Set<PenaltyFramework>()
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (entity is null)
            return Result.Failure(
                new Error("PenaltyFramework.NotFound", "Penalty framework entry not found.", ErrorType.NotFound));

        if (request.Activate)
            entity.Activate();
        else
            entity.Deactivate();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

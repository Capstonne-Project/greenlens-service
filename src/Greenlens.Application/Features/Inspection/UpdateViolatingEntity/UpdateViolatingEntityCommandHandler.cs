using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.UpdateViolatingEntity;

/// <summary>
/// Update violating entity details. Checks for duplicate TaxCode/IdentityNumber.
/// </summary>
/// <remarks>Implements: BR-INS-010 — correction of violator info post biên bản.</remarks>
public sealed class UpdateViolatingEntityCommandHandler(
    IViolatingEntityRepository violatingEntities,
    IUnitOfWork uow,
    ILogger<UpdateViolatingEntityCommandHandler> logger)
    : IRequestHandler<UpdateViolatingEntityCommand, Result>
{
    public async Task<Result> Handle(UpdateViolatingEntityCommand request, CancellationToken ct)
    {
        var entity = await violatingEntities.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (entity is null)
            return Errors.Inspections.ViolatingEntityNotFound;

        // Check TaxCode uniqueness if changing
        if (request.TaxCode is not null && request.TaxCode != entity.TaxCode)
        {
            var existing = await violatingEntities
                .FindByTaxCodeAsync(request.TaxCode, ct).ConfigureAwait(false);
            if (existing is not null && existing.Id != entity.Id)
                return Errors.Inspections.ViolatingEntityDuplicateTaxCode;
        }

        // Check IdentityNumber uniqueness if changing
        if (request.IdentityNumber is not null && request.IdentityNumber != entity.IdentityNumber)
        {
            var existing = await violatingEntities
                .FindByIdentityNumberAsync(request.IdentityNumber, ct).ConfigureAwait(false);
            if (existing is not null && existing.Id != entity.Id)
                return Errors.Inspections.ViolatingEntityDuplicateIdentityNumber;
        }

        entity.Update(
            request.Name,
            request.Address,
            request.TaxCode,
            request.IdentityNumber,
            request.PhoneNumber);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ViolatingEntity {Id} updated (Name={Name})",
            entity.Id, entity.Name);

        return Result.Success();
    }
}

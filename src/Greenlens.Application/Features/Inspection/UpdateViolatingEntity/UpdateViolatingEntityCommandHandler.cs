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
        logger.LogInformation("Getting update violating entity");

        var entity = await violatingEntities.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (entity is null)
        {
            logger.LogWarning("Violating entity not found for id {Id}", request.Id);
            return Errors.Inspections.ViolatingEntityNotFound;
        }

        if (entity.IsDeleted)
        {
            logger.LogWarning("Violating entity {Id} already deleted", request.Id);
            return Errors.Inspections.ViolatingEntityAlreadyDeleted;
        }

        // Check TaxCode uniqueness if changing (include soft-deleted rows)
        if (request.TaxCode is not null && request.TaxCode != entity.TaxCode)
        {
            var taxExists = await violatingEntities
                .TaxCodeExistsAsync(request.TaxCode, entity.Id, ct)
                .ConfigureAwait(false);
            if (taxExists)
            {
                logger.LogWarning("Tax code {TaxCode} already exists", request.TaxCode);
                return Errors.Inspections.ViolatingEntityDuplicateTaxCode;
            }
        }

        // Check IdentityNumber uniqueness if changing
        if (request.IdentityNumber is not null && request.IdentityNumber != entity.IdentityNumber)
        {
            var identityExists = await violatingEntities
                .IdentityNumberExistsAsync(request.IdentityNumber, entity.Id, ct)
                .ConfigureAwait(false);
            if (identityExists)
            {
                logger.LogWarning("Identity number {IdentityNumber} already exists", request.IdentityNumber);
                return Errors.Inspections.ViolatingEntityDuplicateIdentityNumber;
            }
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

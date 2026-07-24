using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.CreateViolatingEntity;

/// <summary>
/// BR-INS-010: Create a violating entity for inspection biên bản.
/// Checks for duplicate TaxCode (Business) or IdentityNumber (Individual).
/// </summary>
public sealed class CreateViolatingEntityCommandHandler(
    IViolatingEntityRepository violatingEntities,
    IUnitOfWork uow,
    ILogger<CreateViolatingEntityCommandHandler> logger)
    : IRequestHandler<CreateViolatingEntityCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateViolatingEntityCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting create violating entity");

        // Check duplicate TaxCode for Business (include soft-deleted — DB unique index is not filtered)
        if (!string.IsNullOrWhiteSpace(request.TaxCode))
        {
            var taxExists = await violatingEntities
                .TaxCodeExistsAsync(request.TaxCode, ct: ct)
                .ConfigureAwait(false);
            if (taxExists)
            {
                logger.LogWarning("Duplicate TaxCode {TaxCode} found", request.TaxCode);
                return Result<Guid>.Failure(Errors.Inspections.ViolatingEntityDuplicateTaxCode);
            }
        }

        // Check duplicate IdentityNumber for Individual
        if (!string.IsNullOrWhiteSpace(request.IdentityNumber))
        {
            var identityExists = await violatingEntities
                .IdentityNumberExistsAsync(request.IdentityNumber, ct: ct)
                .ConfigureAwait(false);
            if (identityExists)
            {
                logger.LogWarning("Duplicate IdentityNumber {IdentityNumber} found", request.IdentityNumber);
                return Result<Guid>.Failure(Errors.Inspections.ViolatingEntityDuplicateIdentityNumber);
            }
        }

        var entity = ViolatingEntity.Create(
            request.Name,
            request.Type,
            request.Address,
            request.TaxCode,
            request.IdentityNumber,
            request.PhoneNumber);

        violatingEntities.Add(entity);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ViolatingEntity created: {Id}, type={Type}, name={Name}",
            entity.Id, entity.Type, entity.Name);

        return Result<Guid>.Success(entity.Id);
    }
}

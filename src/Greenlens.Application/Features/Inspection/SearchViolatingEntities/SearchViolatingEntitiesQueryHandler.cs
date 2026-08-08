using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.SearchViolatingEntities;

/// <summary>Search violating entities by TaxCode, IdentityNumber, or Name partial match.</summary>
public sealed class SearchViolatingEntitiesQueryHandler(
    IViolatingEntityRepository violatingEntities,
    ILogger<SearchViolatingEntitiesQueryHandler> logger)
    : IRequestHandler<SearchViolatingEntitiesQuery, Result<List<ViolatingEntityDto>>>
{
    public async Task<Result<List<ViolatingEntityDto>>> Handle(
        SearchViolatingEntitiesQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting search violating entities");

        var results = new List<ViolatingEntity>();

        // Priority: TaxCode exact match > IdentityNumber exact match > Name partial match
        if (!string.IsNullOrWhiteSpace(request.TaxCode))
        {
            var found = await violatingEntities.FindByTaxCodeAsync(request.TaxCode, ct).ConfigureAwait(false);
            if (found is not null)
            {
                logger.LogWarning("Violating entity found for tax code {TaxCode}", request.TaxCode);
                results.Add(found);
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.IdentityNumber))
        {
            var found = await violatingEntities.FindByIdentityNumberAsync(request.IdentityNumber, ct).ConfigureAwait(false);
            if (found is not null)
            {
                logger.LogWarning("Violating entity found for identity number {IdentityNumber}", request.IdentityNumber);
                results.Add(found);
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.Name))
        {
            results = await violatingEntities.SearchByNameAsync(request.Name, request.MaxResults, ct).ConfigureAwait(false);
            if (results.Count > 0)
            {
                logger.LogWarning("Violating entities found for name {Name}", request.Name);
            }
        }

        var dtos = new List<ViolatingEntityDto>(results.Count);
        foreach (var entity in results)
        {
            var inspectionCount = await violatingEntities
                .CountInspectionsInPeriodAsync(entity.Id, 12, ct).ConfigureAwait(false);

            dtos.Add(new ViolatingEntityDto(
                entity.Id,
                entity.Name,
                entity.Type,
                entity.Address,
                entity.TaxCode,
                entity.IdentityNumber,
                entity.PhoneNumber,
                inspectionCount));
        }

        logger.LogInformation("Violating entities: {ViolatingEntities}", dtos);

        return Result<List<ViolatingEntityDto>>.Success(dtos);
    }
}

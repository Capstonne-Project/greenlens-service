using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Inspection.SearchViolatingEntities;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.GetViolatingEntityById;

/// <summary>Get violating entity details + inspection count (12 months).</summary>
public sealed class GetViolatingEntityByIdQueryHandler(
    IViolatingEntityRepository violatingEntities,
    ILogger<GetViolatingEntityByIdQueryHandler> logger)
    : IRequestHandler<GetViolatingEntityByIdQuery, Result<ViolatingEntityDto>>
{
    public async Task<Result<ViolatingEntityDto>> Handle(
        GetViolatingEntityByIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting violating entity by id");

        var entity = await violatingEntities.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (entity is null)
        {
            logger.LogWarning("Violating entity not found for id {Id}", request.Id);
            return Result<ViolatingEntityDto>.Failure(Errors.Inspections.ViolatingEntityNotFound);
        }

        var inspectionCount = await violatingEntities
            .CountInspectionsInPeriodAsync(entity.Id, 12, ct).ConfigureAwait(false);

        logger.LogInformation("Violating entity: {ViolatingEntity}", entity);

        return Result<ViolatingEntityDto>.Success(new ViolatingEntityDto(
            entity.Id,
            entity.Name,
            entity.Type,
            entity.Address,
            entity.TaxCode,
            entity.IdentityNumber,
            entity.PhoneNumber,
            inspectionCount));
    }
}

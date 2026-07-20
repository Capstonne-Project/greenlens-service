using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.SearchViolatingEntities;

/// <summary>
/// Search violating entities by TaxCode, IdentityNumber, or Name.
/// Used during biên bản creation to find an existing entity and link it.
/// </summary>
public sealed record SearchViolatingEntitiesQuery(
    string? TaxCode = null,
    string? IdentityNumber = null,
    string? Name = null,
    int MaxResults = 20) : IRequest<Result<List<ViolatingEntityDto>>>;

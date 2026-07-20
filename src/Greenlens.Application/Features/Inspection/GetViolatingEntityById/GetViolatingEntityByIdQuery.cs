using Greenlens.Application.Features.Inspection.SearchViolatingEntities;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.GetViolatingEntityById;

/// <summary>Get a violating entity by ID with inspection history count.</summary>
public sealed record GetViolatingEntityByIdQuery(Guid Id) : IRequest<Result<ViolatingEntityDto>>;

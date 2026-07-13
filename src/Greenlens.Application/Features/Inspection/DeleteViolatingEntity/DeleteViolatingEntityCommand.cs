using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.DeleteViolatingEntity;

/// <summary>
/// Soft-delete a ViolatingEntity.
/// Only Admin, LEO, Inspector can perform this.
/// </summary>
public sealed record DeleteViolatingEntityCommand(Guid EntityId) : IRequest<Result>;

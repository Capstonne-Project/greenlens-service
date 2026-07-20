using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Inspection.CreateViolatingEntity;

/// <summary>BR-INS-010: Create a violating entity (individual/household or business).</summary>
public sealed record CreateViolatingEntityCommand(
    string Name,
    ViolatorType Type,
    string? Address = null,
    string? TaxCode = null,
    string? IdentityNumber = null,
    string? PhoneNumber = null) : IRequest<Result<Guid>>;

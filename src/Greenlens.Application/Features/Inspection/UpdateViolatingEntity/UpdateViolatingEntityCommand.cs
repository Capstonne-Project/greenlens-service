using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UpdateViolatingEntity;

/// <summary>
/// Update violating entity details (name, address, TaxCode, IdentityNumber, phone).
/// Only non-null fields are updated.
/// </summary>
/// <remarks>Implements: BR-INS-010 — correction of violator info.</remarks>
public sealed record UpdateViolatingEntityCommand(
    Guid Id,
    string? Name = null,
    string? Address = null,
    string? TaxCode = null,
    string? IdentityNumber = null,
    string? PhoneNumber = null) : IRequest<Result>;

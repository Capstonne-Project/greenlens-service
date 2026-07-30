using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.DeactivatePenaltyFramework;

/// <summary>
/// Deactivate (soft-delete) a penalty framework entry. Does not hard-delete.
/// </summary>
/// <remarks>Implements: BR-ADM-008, BR-ADM-010.</remarks>
public sealed record DeactivatePenaltyFrameworkCommand(Guid Id, bool Activate = false)
    : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "PenaltyFramework";
    string? IAuditable.AuditEntityId => Id.ToString();
}

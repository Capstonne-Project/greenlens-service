using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.Badges.ToggleBadge;

/// <summary>Admin activates or deactivates a badge.</summary>
/// <remarks>Implements: BR-ADM-005, BR-ADM-010.</remarks>
public sealed record ToggleBadgeCommand(Guid Id, bool IsActive) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "Badge";
    string? IAuditable.AuditEntityId => Id.ToString();
}

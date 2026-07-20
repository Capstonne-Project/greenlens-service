using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.UpdateUserRole;

/// <summary>Admin changes a user's role.</summary>
/// <remarks>Implements: BR-ADM-010 — audit logged via pipeline behavior.</remarks>
public sealed record UpdateUserRoleCommand(Guid UserId, UserRole NewRole) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "User";
    string? IAuditable.AuditEntityId => UserId.ToString();
}

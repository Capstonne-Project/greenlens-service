using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ToggleBanUser;

/// <summary>BR-AUTH-015: Toggle ban status for a user.</summary>
/// <remarks>Implements: BR-ADM-010 — audit logged via pipeline behavior.</remarks>
public sealed record ToggleBanUserCommand(Guid UserId) : IRequest<Result<ToggleBanUserResponse>>, IAuditable
{
    string IAuditable.AuditEntityType => "User";
    string? IAuditable.AuditEntityId => UserId.ToString();
}

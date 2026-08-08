using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.DeleteUser;

/// <remarks>Implements: BR-ADM-010 — audit logged via pipeline behavior.</remarks>
public sealed record DeleteUserCommand(Guid UserId) : IRequest<Result<DeleteUserResponse>>, IAuditable
{
    string IAuditable.AuditEntityType => "User";
    string? IAuditable.AuditEntityId => UserId.ToString();
}

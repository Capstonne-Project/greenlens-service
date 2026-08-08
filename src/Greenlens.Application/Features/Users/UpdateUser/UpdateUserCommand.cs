using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Users.UpdateUser;

/// <remarks>Implements: BR-ADM-010 — audit logged in handler with old/new snapshot.</remarks>
public sealed record UpdateUserCommand(
    Guid UserId,
    string? FullName,
    string? PhoneNumber,
    UserRole? Role,
    bool? IsEmailVerified) : IRequest<Result<UpdateUserResponse>>;

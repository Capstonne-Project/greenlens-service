using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Users.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string? FullName,
    string? PhoneNumber,
    UserRole? Role,
    bool? IsEmailVerified) : IRequest<Result<UpdateUserResponse>>;

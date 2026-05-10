using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : IRequest<Result<DeleteUserResponse>>;

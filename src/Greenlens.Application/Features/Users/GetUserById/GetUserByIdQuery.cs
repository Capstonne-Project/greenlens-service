using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDetailDto>>;

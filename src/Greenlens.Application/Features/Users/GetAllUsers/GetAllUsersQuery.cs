using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.GetAllUsers;

public sealed record GetAllUsersQuery() : IRequest<Result<List<UserListItemDto>>>;

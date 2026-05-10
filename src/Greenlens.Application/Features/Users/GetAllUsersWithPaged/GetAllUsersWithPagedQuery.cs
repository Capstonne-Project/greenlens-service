using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Users.GetAllUsersWithPaged;

public sealed record GetAllUsersWithPagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    UserRole? Role = null,
    bool? IsEmailVerified = null) : IRequest<Result<PagedList<UserListItemDto>>>;

using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.UpdateUserRole;

/// <summary>Admin changes a user's role.</summary>
public sealed record UpdateUserRoleCommand(Guid UserId, UserRole NewRole) : IRequest<Result>;

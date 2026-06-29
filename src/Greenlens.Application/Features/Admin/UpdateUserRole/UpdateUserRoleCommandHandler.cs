using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.UpdateUserRole;

/// <summary>
/// Update a user's role. Restricted by role hierarchy.
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-009 — Admin: gán mọi vai trò. DEO/LEO/CM dùng flow riêng (RecruitStaff, CreateCompanyManager).
/// Handler này giữ tách biệt chỉ cho Admin trực tiếp change role.
/// </remarks>
public sealed class UpdateUserRoleCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<UpdateUserRoleCommandHandler> logger) : IRequestHandler<UpdateUserRoleCommand, Result>
{
    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        // BR-AUTH-009: Only Admin can use this handler directly
        if (currentUser.Role != UserRole.Admin.ToString())
            return Errors.Auth.RoleAssignmentNotAllowed;

        var user = await users.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        // Prevent assigning same role
        if (user.Role == request.NewRole)
            return Result.Success();

        var oldRole = user.Role;
        user.ChangeRole(request.NewRole);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogWarning("User {UserId} role changed from {OldRole} to {NewRole} by Admin {AdminId}",
            request.UserId, oldRole, request.NewRole, currentUser.UserId);

        return Result.Success();
    }
}

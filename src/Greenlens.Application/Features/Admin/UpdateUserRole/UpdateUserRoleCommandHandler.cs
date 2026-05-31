using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.UpdateUserRole;

public sealed class UpdateUserRoleCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ILogger<UpdateUserRoleCommandHandler> logger) : IRequestHandler<UpdateUserRoleCommand, Result>
{
    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        // Change user role
        var oldRole = user.Role;
        user.ChangeRole(request.NewRole);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogWarning("User {UserId} role changed from {OldRole} to {NewRole}",
            request.UserId, oldRole, request.NewRole);

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.UpdateUserRole;

public sealed class UpdateUserRoleCommandHandler(
    IUserRepository users,
    IUnitOfWork uow) : IRequestHandler<UpdateUserRoleCommand, Result>
{
    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        user.ChangeRole(request.NewRole);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.AssignLeoToOffice;

/// <summary>
/// Assigns a LEO user to a LocalOffice. Validates user role = LEO.
/// </summary>
/// <remarks>Implements: BR-ORG-002.</remarks>
public sealed class AssignLeoToOfficeCommandHandler(
    ILocalOfficeRepository localOffices,
    IUserRepository users,
    IUnitOfWork uow) : IRequestHandler<AssignLeoToOfficeCommand, Result>
{
    public async Task<Result> Handle(
        AssignLeoToOfficeCommand request,
        CancellationToken cancellationToken)
    {
        var office = await localOffices.GetByIdAsync(request.LocalOfficeId, cancellationToken)
            .ConfigureAwait(false);

        if (office is null)
            return Errors.Organization.LocalOfficeNotFound;

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Users.UserNotFound;

        if (user.Role != UserRole.LEO)
            return Errors.Organization.InvalidRoleForOfficer;

        office.AssignOfficer(request.UserId);
        user.AssignToLocalOffice(request.LocalOfficeId);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

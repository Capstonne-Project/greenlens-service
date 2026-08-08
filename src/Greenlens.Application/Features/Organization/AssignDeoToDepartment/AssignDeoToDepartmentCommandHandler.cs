using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.AssignDeoToDepartment;

/// <summary>
/// Assigns a DEO user to a Department. Validates:
/// 1. Department exists
/// 2. User exists
/// 3. User role == DEO
/// Clears any previous LocalOffice assignment (DEO works at province level).
/// </summary>
/// <remarks>Implements: BR-ORG-001.</remarks>
public sealed class AssignDeoToDepartmentCommandHandler(
    IDepartmentRepository departments,
    IUserRepository users,
    IUnitOfWork uow,
    ILogger<AssignDeoToDepartmentCommandHandler> logger) : IRequestHandler<AssignDeoToDepartmentCommand, Result>
{
    public async Task<Result> Handle(
        AssignDeoToDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await departments.GetByIdAsync(request.DepartmentId, cancellationToken)
            .ConfigureAwait(false);

        if (department is null)
        {
            logger.LogWarning("Department {DepartmentId} not found", request.DepartmentId);
            return Errors.Organization.DepartmentNotFound;
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return Errors.Users.UserNotFound;
        }
        if (user.Role != UserRole.DEO)
        {
            logger.LogWarning("User {UserId} is not a DEO", request.UserId);
            return Errors.Organization.InvalidRoleForDeo;
        }
        user.AssignToDepartment(request.DepartmentId);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("DEO {UserId} assigned to department {DeptId}",
            request.UserId, request.DepartmentId);

        return Result.Success();
    }
}

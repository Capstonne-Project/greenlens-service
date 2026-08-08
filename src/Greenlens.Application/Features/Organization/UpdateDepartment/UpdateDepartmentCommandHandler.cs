using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.UpdateDepartment;

public sealed class UpdateDepartmentCommandHandler(
    IDepartmentRepository departments,
    IUnitOfWork uow,
    ILogger<UpdateDepartmentCommandHandler> logger) : IRequestHandler<UpdateDepartmentCommand, Result>
{
    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var dept = await departments.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (dept is null)
        {
            logger.LogWarning("Department not found for ID {Id}", request.Id);
            return Errors.Organization.DepartmentNotFound;
        }

        // Update department name
        dept.Update(request.Name);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Department {DeptId} updated", request.Id);

        return Result.Success();
    }
}

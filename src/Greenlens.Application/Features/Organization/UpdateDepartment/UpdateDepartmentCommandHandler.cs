using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateDepartment;

public sealed class UpdateDepartmentCommandHandler(
    IDepartmentRepository departments,
    IUnitOfWork uow) : IRequestHandler<UpdateDepartmentCommand, Result>
{
    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var dept = await departments.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (dept is null)
            return Errors.Organization.DepartmentNotFound;

        dept.Update(request.Name);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}

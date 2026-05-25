using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateDepartment;

/// <summary>
/// Creates a Department of Environmental Management for a Province.
/// </summary>
/// <remarks>Implements: BR-ORG-001.</remarks>
public sealed class CreateDepartmentCommandHandler(
    IDepartmentRepository departments,
    IProvinceRepository provinces,
    IUnitOfWork uow) : IRequestHandler<CreateDepartmentCommand, Result<CreateDepartmentResponse>>
{
    public async Task<Result<CreateDepartmentResponse>> Handle(
        CreateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify province exists
        var province = await provinces.GetByCodeAsync(request.ProvinceCode, cancellationToken)
            .ConfigureAwait(false);

        if (province is null)
            return Errors.Organization.ProvinceNotFound;

        // Check uniqueness: one department per province
        var exists = await departments.ExistsByProvinceCodeAsync(request.ProvinceCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
            return Errors.Organization.DepartmentAlreadyExists;

        var department = Department.Create(request.Name, request.ProvinceCode);
        departments.Add(department);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateDepartmentResponse(department.Id, department.Name, department.ProvinceCode);
    }
}

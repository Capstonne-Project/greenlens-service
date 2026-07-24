using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateDepartment;

/// <summary>
/// Creates a Department of Environmental Management for a Province.
/// </summary>
/// <remarks>Implements: BR-ORG-001.</remarks>
public sealed class CreateDepartmentCommandHandler(
    IDepartmentRepository departments,
    IProvinceRepository provinces,
    IUnitOfWork uow,
    ILogger<CreateDepartmentCommandHandler> logger) : IRequestHandler<CreateDepartmentCommand, Result<CreateDepartmentResponse>>
{
    public async Task<Result<CreateDepartmentResponse>> Handle(
        CreateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating department {Name} for province {ProvinceCode}", request.Name, request.ProvinceCode);

        // Verify province exists
        var province = await provinces.GetByCodeAsync(request.ProvinceCode, cancellationToken)
            .ConfigureAwait(false);

        if (province is null)
        {
            logger.LogWarning("Province {ProvinceCode} not found", request.ProvinceCode);
            return Errors.Organization.ProvinceNotFound;
        }

        // Check uniqueness: one department per province
        var exists = await departments.ExistsByProvinceCodeAsync(request.ProvinceCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            logger.LogWarning("Department {Name} for province {ProvinceCode} already exists", request.Name, request.ProvinceCode);
            return Errors.Organization.DepartmentAlreadyExists;
        }

        var department = Department.Create(request.Name, request.ProvinceCode);
        departments.Add(department);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Department {DeptId} created for province {ProvinceCode}",
            department.Id, request.ProvinceCode);

        return new CreateDepartmentResponse(department.Id, department.Name, department.ProvinceCode);
    }
}

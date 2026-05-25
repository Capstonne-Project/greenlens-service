using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateLocalOffice;

/// <summary>
/// Admin onboards a new ward/commune. Creates LocalOffice, links to Department and Ward.
/// </summary>
/// <remarks>Implements: BR-ORG-002, BR-ADM-011.</remarks>
public sealed class CreateLocalOfficeCommandHandler(
    ILocalOfficeRepository localOffices,
    IDepartmentRepository departments,
    IWardRepository wards,
    IUnitOfWork uow) : IRequestHandler<CreateLocalOfficeCommand, Result<CreateLocalOfficeResponse>>
{
    public async Task<Result<CreateLocalOfficeResponse>> Handle(
        CreateLocalOfficeCommand request,
        CancellationToken cancellationToken)
    {
        // Verify department exists
        var department = await departments.GetByIdAsync(request.DepartmentId, cancellationToken)
            .ConfigureAwait(false);

        if (department is null)
            return Errors.Organization.DepartmentNotFound;

        // Verify ward exists
        var ward = await wards.GetByCodeAsync(request.WardCode, cancellationToken)
            .ConfigureAwait(false);

        if (ward is null)
            return Errors.Organization.WardNotFound;

        // Check uniqueness: one office per ward
        var exists = await localOffices.ExistsByWardCodeAsync(request.WardCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
            return Errors.Organization.LocalOfficeAlreadyExists;

        var office = LocalOffice.Create(
            request.Name,
            request.DepartmentId,
            request.WardCode,
            request.OfficerId);

        localOffices.Add(office);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateLocalOfficeResponse(office.Id, office.Name, office.DepartmentId, office.WardCode);
    }
}

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetDepartmentById;

public sealed class GetDepartmentByIdQueryHandler(
    IDepartmentRepository departments)
    : IRequestHandler<GetDepartmentByIdQuery, Result<DepartmentDetailResponse>>
{
    public async Task<Result<DepartmentDetailResponse>> Handle(
        GetDepartmentByIdQuery request, CancellationToken ct)
    {
        var dept = await departments.QueryAsNoTracking()
            .Include(d => d.Province)
            .Include(d => d.LocalOffices).ThenInclude(o => o.Ward)
            .Include(d => d.LocalOffices).ThenInclude(o => o.Officer)
            .Include(d => d.LocalOffices).ThenInclude(o => o.Teams)
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (dept is null)
            return Errors.Organization.DepartmentNotFound;

        var offices = dept.LocalOffices.Select(o => new OfficeInDepartment(
            o.Id, o.Name, o.WardCode,
            o.Ward?.Name,
            o.OfficerId,
            o.Officer?.FullName,
            o.IsOnboarded,
            o.Teams.Count)).ToList();

        return new DepartmentDetailResponse(
            dept.Id, dept.Name, dept.ProvinceCode,
            dept.Province?.Name,
            dept.IsActive, offices,
            dept.CreatedAt, dept.UpdatedAt);
    }
}

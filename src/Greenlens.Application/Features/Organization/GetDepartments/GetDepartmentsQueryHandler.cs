using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetDepartments;

public sealed class GetDepartmentsQueryHandler(
    IDepartmentRepository departments)
    : IRequestHandler<GetDepartmentsQuery, Result<GetDepartmentsResponse>>
{
    public async Task<Result<GetDepartmentsResponse>> Handle(
        GetDepartmentsQuery request, CancellationToken ct)
    {
        var query = departments.QueryAsNoTracking()
            .Include(d => d.Province)
            .Include(d => d.LocalOffices)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(d => d.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderBy(d => d.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DepartmentItem(
                d.Id,
                d.Name,
                d.ProvinceCode,
                d.Province != null ? d.Province.Name : null,
                d.IsActive,
                d.LocalOffices.Count,
                d.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetDepartmentsResponse(items, totalCount, request.Page, request.PageSize);
    }
}

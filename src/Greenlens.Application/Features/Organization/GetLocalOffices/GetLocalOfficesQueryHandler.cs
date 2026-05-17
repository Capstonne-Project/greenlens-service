using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetLocalOffices;

public sealed class GetLocalOfficesQueryHandler(
    ILocalOfficeRepository offices)
    : IRequestHandler<GetLocalOfficesQuery, Result<GetLocalOfficesResponse>>
{
    public async Task<Result<GetLocalOfficesResponse>> Handle(
        GetLocalOfficesQuery request, CancellationToken ct)
    {
        var query = offices.QueryAsNoTracking()
            .Include(o => o.Department)
            .Include(o => o.Ward)
            .Include(o => o.Officer)
            .Include(o => o.Teams)
            .AsQueryable();

        if (request.DepartmentId.HasValue)
            query = query.Where(o => o.DepartmentId == request.DepartmentId.Value);
        if (request.IsOnboarded.HasValue)
            query = query.Where(o => o.IsOnboarded == request.IsOnboarded.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderBy(o => o.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new LocalOfficeItem(
                o.Id, o.Name, o.DepartmentId,
                o.Department != null ? o.Department.Name : null,
                o.WardCode,
                o.Ward != null ? o.Ward.Name : null,
                o.OfficerId,
                o.Officer != null ? o.Officer.FullName : null,
                o.IsOnboarded, o.Teams.Count, o.CreatedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new GetLocalOfficesResponse(items, totalCount, request.Page, request.PageSize);
    }
}

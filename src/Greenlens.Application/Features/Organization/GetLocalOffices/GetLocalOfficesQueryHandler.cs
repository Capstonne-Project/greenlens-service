using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetLocalOffices;

public sealed class GetLocalOfficesQueryHandler(
    ILocalOfficeRepository offices,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetLocalOfficesQueryHandler> logger)
    : IRequestHandler<GetLocalOfficesQuery, Result<GetLocalOfficesResponse>>
{
    public async Task<Result<GetLocalOfficesResponse>> Handle(
        GetLocalOfficesQuery request, CancellationToken ct)
    {
        var actor = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (actor is null)
            return Errors.Users.UserNotFound;

        var departmentFilter = DepartmentContextResolver.ResolveDepartmentFilter(
            actor, request.DepartmentId);

        if (actor.Role == UserRole.DEO && !departmentFilter.HasValue)
        {
            logger.LogWarning("User {UserId} denied access to any department", currentUser.UserId);
            return Errors.Organization.DepartmentNotFound;
        }

        logger.LogInformation("Getting local offices for department {DepartmentId}", departmentFilter);

        var query = offices.QueryAsNoTracking()
            .Include(o => o.Department)
            .Include(o => o.Ward)
            .Include(o => o.Officer)
            .Include(o => o.Teams)
            .AsQueryable();

        if (departmentFilter.HasValue)
        {
            logger.LogInformation("Filtering local offices by department ID: {DepartmentId}", departmentFilter.Value);
            query = query.Where(o => o.DepartmentId == departmentFilter.Value);
        }

        if (request.IsOnboarded.HasValue)
        {
            logger.LogInformation("Filtering local offices by onboarded status: {IsOnboarded}", request.IsOnboarded.Value);
            query = query.Where(o => o.IsOnboarded == request.IsOnboarded.Value);
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

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

        logger.LogInformation("Lấy danh sách đơn vị hành chính thành công. Số lượng: {Count}", items.Count);
        return new GetLocalOfficesResponse(items, pagination);
    }
}

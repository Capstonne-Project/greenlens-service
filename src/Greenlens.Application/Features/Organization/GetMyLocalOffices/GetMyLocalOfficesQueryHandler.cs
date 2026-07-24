using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetMyLocalOffices;

/// <summary>
/// Returns local offices under the department the current officer belongs to.
/// DEO → User.DepartmentId → LocalOffices.
/// LEO → User.LocalOfficeId → LocalOffice.DepartmentId → LocalOffices.
/// </summary>
/// <remarks>Implements: BR-ORG-001 (department scope), BR-ORG-002 (office listing).</remarks>
public sealed class GetMyLocalOfficesQueryHandler(
    IUserRepository users,
    ILocalOfficeRepository localOffices,
    ICurrentUser currentUser,
    ILogger<GetMyLocalOfficesQueryHandler> logger)
    : IRequestHandler<GetMyLocalOfficesQuery, Result<GetMyLocalOfficesResponse>>
{
    public async Task<Result<GetMyLocalOfficesResponse>> Handle(
        GetMyLocalOfficesQuery request,
        CancellationToken ct)
    {
        // 1. Resolve the officer's department
        var deptInfo = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new
            {
                DepartmentId = u.DepartmentId ?? u.LocalOffice!.DepartmentId,
                DepartmentName = u.Department != null
                    ? u.Department.Name
                    : u.LocalOffice!.Department!.Name,
                ProvinceCode = u.Department != null
                    ? u.Department.ProvinceCode
                    : u.LocalOffice!.Department!.ProvinceCode
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (deptInfo is null || deptInfo.DepartmentId == Guid.Empty)
        {
            logger.LogWarning("Department not found for user ID {UserId}", currentUser.UserId);
            return Errors.Organization.DepartmentNotFound;
        }

        // 2. Build base query
        var baseQuery = localOffices.QueryAsNoTracking()
            .Where(o => o.DepartmentId == deptInfo.DepartmentId);

        // 3. Apply search filter (office name, ward name, officer name)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(o =>
                o.Name.ToLower().Contains(keyword) ||
                (o.Ward != null && o.Ward.Name.ToLower().Contains(keyword)) ||
                (o.Officer != null && o.Officer.FullName.ToLower().Contains(keyword)));
        }

        // 4. Apply isOnboarded filter
        if (request.IsOnboarded.HasValue)
        {
            logger.LogInformation("Filtering local offices by onboarded status: {IsOnboarded}", request.IsOnboarded.Value);
            baseQuery = baseQuery.Where(o => o.IsOnboarded == request.IsOnboarded.Value);
        }

        // 5. Count total
        var totalItems = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalItems);

        // 6. Apply sorting
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var orderedQuery = sortBy switch
        {
            "name" => request.SortDesc
                ? baseQuery.OrderByDescending(o => o.Name)
                : baseQuery.OrderBy(o => o.Name),
            "wardname" => request.SortDesc
                ? baseQuery.OrderByDescending(o => o.Ward != null ? o.Ward.Name : "")
                : baseQuery.OrderBy(o => o.Ward != null ? o.Ward.Name : ""),
            "officername" => request.SortDesc
                ? baseQuery.OrderByDescending(o => o.Officer != null ? o.Officer.FullName : "")
                : baseQuery.OrderBy(o => o.Officer != null ? o.Officer.FullName : ""),
            "teamcount" => request.SortDesc
                ? baseQuery.OrderByDescending(o => o.Teams.Count)
                : baseQuery.OrderBy(o => o.Teams.Count),
            "createdat" => request.SortDesc
                ? baseQuery.OrderByDescending(o => o.CreatedAt)
                : baseQuery.OrderBy(o => o.CreatedAt),
            _ => baseQuery.OrderBy(o => o.Name) // default sort by name ASC
        };

        // 7. Paginate & project
        var offices = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new MyLocalOfficeItem(
                o.Id,
                o.Name,
                o.WardCode,
                o.Ward != null ? o.Ward.Name : null,
                o.OfficerId,
                o.Officer != null ? o.Officer.FullName : null,
                o.IsOnboarded,
                o.Teams.Count,
                o.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Officer {UserId} fetched {Count} local offices for department {DepartmentId} (page {Page}, search: {Search}, isOnboarded: {IsOnboarded}, sort: {SortBy})",
            currentUser.UserId, offices.Count, deptInfo.DepartmentId, request.Page,
            request.Search, request.IsOnboarded, request.SortBy);

        return new GetMyLocalOfficesResponse(
            deptInfo.DepartmentId,
            deptInfo.DepartmentName,
            deptInfo.ProvinceCode,
            offices,
            pagination);
    }
}

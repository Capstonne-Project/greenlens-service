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

namespace Greenlens.Application.Features.Organization.GetDepartments;

public sealed class GetDepartmentsQueryHandler(
    IDepartmentRepository departments,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetDepartmentsQueryHandler> logger)
    : IRequestHandler<GetDepartmentsQuery, Result<GetDepartmentsResponse>>
{
    public async Task<Result<GetDepartmentsResponse>> Handle(
        GetDepartmentsQuery request, CancellationToken ct)
    {
        var actor = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (actor is null)
        {
            logger.LogWarning("User {UserId} not found", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        var query = departments.QueryAsNoTracking()
            .Include(d => d.Province)
            .Include(d => d.LocalOffices)
            .AsQueryable();

        if (actor.Role == UserRole.DEO)
        {
            if (!actor.DepartmentId.HasValue)
            {
                logger.LogWarning("User {UserId} denied access to any department", currentUser.UserId);
                return Errors.Organization.DepartmentNotFound;
            }

            query = query.Where(d => d.Id == actor.DepartmentId.Value);
        }

        if (request.IsActive.HasValue)
        {
            logger.LogInformation("Filtering departments by active status: {IsActive}", request.IsActive.Value);
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

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

        logger.LogInformation("Lấy danh sách phòng ban thành công. Số lượng: {Count}", items.Count);
        return new GetDepartmentsResponse(items, pagination);
    }
}

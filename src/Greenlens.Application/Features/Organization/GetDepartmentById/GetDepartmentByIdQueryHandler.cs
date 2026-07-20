using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetDepartmentById;

public sealed class GetDepartmentByIdQueryHandler(
    IDepartmentRepository departments,
    IUserRepository users,
    ILogger<GetDepartmentByIdQueryHandler> logger)
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
        {
            logger.LogWarning("Không tìm thấy phòng ban với ID: {DepartmentId}", request.Id);
            return Errors.Organization.DepartmentNotFound;
        }

        // ── Tìm DEO phụ trách department này ──
        var deo = await users.QueryAsNoTracking()
            .Where(u => u.DepartmentId == dept.Id && u.Role == UserRole.DEO)
            .Select(u => new DeoOfficerInfo(
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.AvatarUrl))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var offices = dept.LocalOffices.Select(o => new OfficeInDepartment(
            o.Id, o.Name, o.WardCode,
            o.Ward?.Name,
            o.OfficerId,
            o.Officer?.FullName,
            o.IsOnboarded,
            o.Teams.Count)).ToList();

        logger.LogInformation("Lấy thông tin chi tiết phòng ban thành công. Tên phòng ban: {DepartmentName}", dept.Name);
        return new DepartmentDetailResponse(
            dept.Id, dept.Name, dept.ProvinceCode,
            dept.Province?.Name,
            dept.IsActive, deo, offices,
            dept.CreatedAt, dept.UpdatedAt);
    }
}

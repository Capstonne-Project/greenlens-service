using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetLocalOfficeById;

public sealed class GetLocalOfficeByIdQueryHandler(
    ILocalOfficeRepository offices,
    ILogger<GetLocalOfficeByIdQueryHandler> logger)
    : IRequestHandler<GetLocalOfficeByIdQuery, Result<LocalOfficeDetailResponse>>
{
    public async Task<Result<LocalOfficeDetailResponse>> Handle(
        GetLocalOfficeByIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting local office by ID {Id}", request.Id);

        var office = await offices.QueryAsNoTracking()
            .Include(o => o.Department)
            .Include(o => o.Ward)
            .Include(o => o.Officer)
            .Include(o => o.Teams).ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (office is null)
        {
            logger.LogWarning("Không tìm thấy văn phòng với ID: {OfficeId}", request.Id);
            return Errors.Organization.OfficeNotFound;
        }

        var teams = office.Teams.Select(t => new TeamInOffice(
            t.Id, t.Name, t.TeamType, t.IsActive, t.Members.Count)).ToList();

        logger.LogInformation("Lấy thông tin chi tiết văn phòng thành công. Tên văn phòng: {OfficeName}", office.Name);
        return new LocalOfficeDetailResponse(
            office.Id, office.Name, office.DepartmentId,
            office.Department?.Name, office.WardCode,
            office.Ward?.Name, office.OfficerId,
            office.Officer?.FullName, office.IsOnboarded,
            teams, office.CreatedAt, office.UpdatedAt);
    }
}

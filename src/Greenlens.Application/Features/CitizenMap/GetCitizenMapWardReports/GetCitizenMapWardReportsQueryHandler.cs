using Greenlens.Application.Common;
using Greenlens.Application.Common.CitizenMap;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWardReports;

/// <summary>
/// Citizen map bước 3: khi công dân bấm đúng 1 phường/xã, trả toàn bộ report pin (Verified+,
/// không ẩn) thuộc phường đó cho FE vẽ lên bản đồ.
/// </summary>
public sealed class GetCitizenMapWardReportsQueryHandler(
    IWardRepository wardsRepo,
    IReportRepository reports,
    ILogger<GetCitizenMapWardReportsQueryHandler> logger)
    : IRequestHandler<GetCitizenMapWardReportsQuery, Result<GetCitizenMapWardReportsResponse>>
{
    private const int MaxPins = 500;

    public async Task<Result<GetCitizenMapWardReportsResponse>> Handle(
        GetCitizenMapWardReportsQuery request,
        CancellationToken cancellationToken)
    {
        var wardCode = request.WardCode.Trim();

        var ward = await wardsRepo.GetByCodeAsync(wardCode, cancellationToken).ConfigureAwait(false);
        if (ward is null)
        {
            logger.LogWarning("Citizen map: ward not found {WardCode}", wardCode);
            return Errors.Catalog.WardNotFound;
        }

        var items = await reports.QueryAsNoTracking()
            .Where(r => r.WardCode == wardCode)
            .Where(r => !r.IsHidden)
            .Where(r => CitizenMapReportStatuses.Visible.Contains(r.Status))
            .OrderByDescending(r => r.CreatedAt)
            .Take(MaxPins)
            .Select(r => new CitizenMapWardReportPinDto(
                r.Id,
                r.Code,
                r.Latitude,
                r.Longitude,
                r.Severity,
                r.Category!.Code,
                r.Category!.NameVi,
                r.Category!.IconUrl,
                r.Description,
                r.Address,
                r.ReporterCount,
                r.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                r.Status,
                r.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Citizen map: lấy {Count} report pin cho phường {WardCode}", items.Count, wardCode);

        return new GetCitizenMapWardReportsResponse(ward.Code, ward.Name, items);
    }
}

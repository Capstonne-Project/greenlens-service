using Greenlens.Application.Common;
using Greenlens.Application.Common.CitizenMap;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWards;

/// <summary>
/// Citizen map bước 2: khi công dân bấm vào 1 tỉnh, trả toàn bộ phường/xã của tỉnh đó kèm boundary
/// và mức độ rủi ro (5 cấp màu) tính theo số báo cáo đang active — FE chỉ vẽ lại đúng theo dữ liệu
/// trả về, không tự tính ngưỡng.
/// </summary>
public sealed class GetCitizenMapWardsQueryHandler(
    IProvinceRepository provinces,
    IWardRepository wards,
    IReportRepository reports,
    IWardBoundaryLookupService boundaryLookup,
    ILogger<GetCitizenMapWardsQueryHandler> logger)
    : IRequestHandler<GetCitizenMapWardsQuery, Result<GetCitizenMapWardsResponse>>
{
    public async Task<Result<GetCitizenMapWardsResponse>> Handle(
        GetCitizenMapWardsQuery request,
        CancellationToken cancellationToken)
    {
        var provinceCode = request.ProvinceCode.Trim();

        var province = await provinces.GetByCodeAsync(provinceCode, cancellationToken).ConfigureAwait(false);
        if (province is null)
        {
            logger.LogWarning("Citizen map: province not found {ProvinceCode}", provinceCode);
            return Errors.Catalog.ProvinceNotFound;
        }

        var wardUnitAbbreviations = await wards.QueryAsNoTracking()
            .Where(w => w.ProvinceCode == provinceCode)
            .Select(w => new { w.Code, Abbreviation = w.AdministrativeUnit!.Abbreviation })
            .ToDictionaryAsync(w => w.Code, w => w.Abbreviation, cancellationToken)
            .ConfigureAwait(false);

        var provinceWards = await wards.GetByProvinceAsync(provinceCode, cancellationToken).ConfigureAwait(false);
        var boundaries = await boundaryLookup
            .GetWardBoundaryGeoJsonByProvinceAsync(provinceCode, cancellationToken)
            .ConfigureAwait(false);

        var activeCountByWard = await reports.QueryAsNoTracking()
            .Where(r => r.ProvinceCode == provinceCode)
            .Where(r => r.WardCode != null)
            .Where(r => !r.IsHidden)
            .Where(r => CitizenMapReportStatuses.Active.Contains(r.Status))
            .GroupBy(r => r.WardCode!)
            .Select(g => new { WardCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.WardCode, g => g.Count, cancellationToken)
            .ConfigureAwait(false);

        var items = provinceWards
            .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
            .Select(w =>
            {
                var activeCount = activeCountByWard.GetValueOrDefault(w.Code);
                var level = WardRiskLevelCalculator.FromActiveReportCount(activeCount);
                return new CitizenMapWardDto(
                    w.Code,
                    w.Name,
                    wardUnitAbbreviations.GetValueOrDefault(w.Code),
                    boundaries.GetValueOrDefault(w.Code),
                    activeCount,
                    level,
                    WardRiskLevelCalculator.ColorHexFor(level));
            })
            .ToList();

        logger.LogInformation(
            "Citizen map: lấy {Count} phường/xã cho tỉnh {ProvinceCode}",
            items.Count, provinceCode);

        return new GetCitizenMapWardsResponse(province.Code, province.Name, items);
    }
}

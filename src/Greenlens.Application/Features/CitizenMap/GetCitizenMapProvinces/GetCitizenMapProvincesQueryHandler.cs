using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapProvinces;

/// <summary>
/// Citizen map bước 1: toàn bộ tỉnh/thành kèm boundary GeoJSON, để FE tô ranh giới ngay khi vào
/// trang (mỗi tỉnh 1 màu cố định do FE tự sinh theo <c>Code</c> — không mang ý nghĩa dữ liệu).
/// </summary>
public sealed class GetCitizenMapProvincesQueryHandler(
    IProvinceRepository provinces,
    IWardBoundaryLookupService boundaryLookup,
    ILogger<GetCitizenMapProvincesQueryHandler> logger)
    : IRequestHandler<GetCitizenMapProvincesQuery, Result<GetCitizenMapProvincesResponse>>
{
    public async Task<Result<GetCitizenMapProvincesResponse>> Handle(
        GetCitizenMapProvincesQuery request,
        CancellationToken cancellationToken)
    {
        var allProvinces = await provinces.GetAllForListAsync(cancellationToken).ConfigureAwait(false);
        var boundaries = await boundaryLookup
            .GetAllProvinceBoundaryGeoJsonAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = allProvinces
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => new CitizenMapProvinceDto(
                p.Code,
                p.Name,
                boundaries.GetValueOrDefault(p.Code)))
            .ToList();

        logger.LogInformation(
            "Citizen map: lấy danh sách {Count} tỉnh, {WithBoundary} tỉnh có boundary",
            items.Count, items.Count(i => i.GeoJson is not null));

        return new GetCitizenMapProvincesResponse(items);
    }
}

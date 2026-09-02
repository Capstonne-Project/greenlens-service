using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapProvinces;

/// <summary>
/// Bước 1 của citizen map: toàn bộ 34 tỉnh/thành kèm boundary GeoJSON để tô ranh giới ngay khi
/// vào trang. Không nhận tham số — luôn trả toàn quốc (dataset nhỏ, ~34 rows).
/// </summary>
public sealed record GetCitizenMapProvincesQuery : IRequest<Result<GetCitizenMapProvincesResponse>>;

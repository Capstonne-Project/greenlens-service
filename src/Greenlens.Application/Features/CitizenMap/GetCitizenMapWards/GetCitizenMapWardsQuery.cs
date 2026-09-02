using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWards;

/// <summary>
/// Bước 2 của citizen map: toàn bộ phường/xã của 1 tỉnh, kèm boundary GeoJSON và mức độ rủi ro
/// (5 cấp, tính theo số báo cáo đang active trong phường) để FE tô màu drill-down.
/// </summary>
public sealed record GetCitizenMapWardsQuery(string ProvinceCode) : IRequest<Result<GetCitizenMapWardsResponse>>;

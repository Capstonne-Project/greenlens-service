using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWardReports;

/// <summary>
/// Bước 3 của citizen map: khi công dân bấm vào đúng 1 phường/xã, trả toàn bộ điểm báo cáo ô
/// nhiễm (rác thải) thuộc phường đó để vẽ pin lên bản đồ.
/// </summary>
public sealed record GetCitizenMapWardReportsQuery(string WardCode)
    : IRequest<Result<GetCitizenMapWardReportsResponse>>;

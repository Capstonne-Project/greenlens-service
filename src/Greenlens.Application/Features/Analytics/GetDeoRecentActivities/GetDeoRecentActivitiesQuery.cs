using Greenlens.Application.Features.Analytics.GetAdminRecentActivities;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoRecentActivities;

public sealed record GetDeoRecentActivitiesQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<List<RecentActivityItem>>>;

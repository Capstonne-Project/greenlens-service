using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminRecentActivities;

public sealed record GetAdminRecentActivitiesQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<List<RecentActivityItem>>>;

public sealed record RecentActivityItem(
    DateTime Time,
    string Type,
    string Description);

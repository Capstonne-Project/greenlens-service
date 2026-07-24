using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyRecentActivities;

public sealed record GetCompanyRecentActivitiesQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<CompanyRecentActivityItem>>>;

public sealed record CompanyRecentActivityItem(
    DateTime Time,
    string Type,
    string Description);

using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminPollutionAnalytics;

public sealed record GetAdminPollutionAnalyticsQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<PollutionAnalyticsItem>>>;

public sealed record PollutionAnalyticsItem(
    string Category,
    int Count);

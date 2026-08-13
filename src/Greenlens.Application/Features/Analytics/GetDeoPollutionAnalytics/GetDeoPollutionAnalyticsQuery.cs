using Greenlens.Application.Features.Analytics.GetAdminPollutionAnalytics;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoPollutionAnalytics;

public sealed record GetDeoPollutionAnalyticsQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<PollutionAnalyticsItem>>>;

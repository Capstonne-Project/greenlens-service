using Greenlens.Application.Features.Analytics.GetAdminReportTrend;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoReportTrend;

public sealed record GetDeoReportTrendQuery(
    ReportTrendGroupBy GroupBy = ReportTrendGroupBy.Day,
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ReportTrendItem>>>;

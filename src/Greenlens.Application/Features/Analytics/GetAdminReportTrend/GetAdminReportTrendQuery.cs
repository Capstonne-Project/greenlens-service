using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminReportTrend;

public enum ReportTrendGroupBy
{
    Day,
    Week,
    Month
}

public sealed record GetAdminReportTrendQuery(
    ReportTrendGroupBy GroupBy = ReportTrendGroupBy.Day,
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ReportTrendItem>>>;

public sealed record ReportTrendItem(
    DateOnly Date,
    int Created,
    int Resolved);

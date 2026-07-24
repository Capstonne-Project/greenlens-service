using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminReportFunnel;

public sealed record GetAdminReportFunnelQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ReportFunnelStage>>>;

public sealed record ReportFunnelStage(
    string Stage,
    int Count);

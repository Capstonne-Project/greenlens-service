using Greenlens.Application.Features.Analytics.GetAdminReportFunnel;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoReportFunnel;

public sealed record GetDeoReportFunnelQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ReportFunnelStage>>>;

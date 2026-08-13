using Greenlens.Application.Features.Analytics.GetAdminReportStatusDistribution;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoReportStatusDistribution;

public sealed record GetDeoReportStatusDistributionQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ReportStatusDistributionItem>>>;

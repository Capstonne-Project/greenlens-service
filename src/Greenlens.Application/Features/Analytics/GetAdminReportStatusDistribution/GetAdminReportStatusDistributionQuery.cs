using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminReportStatusDistribution;

public sealed record GetAdminReportStatusDistributionQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ReportStatusDistributionItem>>>;

public sealed record ReportStatusDistributionItem(
    ReportStatus Status,
    int Count,
    decimal Percentage);

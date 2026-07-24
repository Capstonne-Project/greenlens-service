using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyUpcomingDeadlines;

public sealed record GetCompanyUpcomingDeadlinesQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<UpcomingDeadlineItem>>>;

public sealed record UpcomingDeadlineItem(
    Guid ReportId,
    string Code,
    string CategoryName,
    Severity Severity,
    DateTime SlaResolveDueAt,
    decimal HoursRemaining);

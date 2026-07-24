using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminOfficerPerformance;

public sealed record GetAdminOfficerPerformanceQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<OfficerPerformanceItem>>>;

public sealed record OfficerPerformanceItem(
    Guid OfficerId,
    string OfficerName,
    int VerifiedReports,
    decimal AverageHours,
    decimal SlaRate,
    decimal Score);

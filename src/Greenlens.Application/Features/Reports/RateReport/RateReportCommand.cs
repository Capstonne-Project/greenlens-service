using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.RateReport;

/// <summary>
/// Citizen rates the resolved/closed report.
/// </summary>
/// <remarks>Implements: BR-REP-018.</remarks>
public sealed record RateReportCommand(
    Guid ReportId,
    bool IsSatisfied,
    int? Rating,
    string? Comment) : IRequest<Result<RateReportResponse>>;

public sealed record RateReportResponse(Guid SatisfactionId);

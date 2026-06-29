using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.EscalateReport;

/// <summary>
/// BR-ORG-016: LEO escalates a verified report to Department (DEO) queue.
/// Used when the report is on a city-level route (e.g. CITENCO territory).
/// </summary>
public sealed record EscalateReportCommand(Guid ReportId, string Reason) : IRequest<Result>;

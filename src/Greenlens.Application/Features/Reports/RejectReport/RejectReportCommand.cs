using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.RejectReport;

/// <summary>LEO/DEO rejects a submitted report. BR-REP-022.</summary>
public sealed record RejectReportCommand(
    Guid ReportId,
    string Reason) : IRequest<Result>;

using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.ResolveReport;

/// <summary>
/// Cleanup Team marks their assignment as completed. BR-REP-014, BR-CLN-005.
/// Report only transitions to Resolved when ALL team assignments are completed.
/// </summary>
public sealed record ResolveReportCommand(
    Guid ReportId,
    Guid TeamId,
    List<string> AfterImageUrls) : IRequest<Result>;

using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.ReDispatchReport;

/// <summary>DEO re-dispatches a task from one ward to another. Report must be Dispatched (not yet assigned by LEO).</summary>
public sealed record ReDispatchReportCommand(
    Guid ReportId,
    Guid NewLocalOfficeId,
    string? Note) : IRequest<Result>;

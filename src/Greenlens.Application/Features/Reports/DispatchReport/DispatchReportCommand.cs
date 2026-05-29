using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.DispatchReport;

/// <summary>DEO dispatches a verified report to a target LocalOffice (ward/commune).</summary>
public sealed record DispatchReportCommand(
    Guid ReportId,
    Guid TargetLocalOfficeId,
    string? Note) : IRequest<Result>;

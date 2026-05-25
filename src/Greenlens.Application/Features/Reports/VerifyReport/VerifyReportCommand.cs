using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.VerifyReport;

/// <summary>LEO/DEO verifies a submitted report. BR-OFF-001, BR-OFF-003, BR-REP-020.</summary>
public sealed record VerifyReportCommand(
    Guid ReportId,
    Severity? OverrideSeverity,
    Guid? OverrideCategoryId) : IRequest<Result>;

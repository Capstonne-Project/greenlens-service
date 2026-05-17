using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.ForceUpdateReportStatus;

/// <summary>Admin force-updates a report's status (e.g. escalate, fix data).</summary>
public sealed record ForceUpdateReportStatusCommand(
    Guid ReportId,
    ReportStatus NewStatus,
    string Reason) : IRequest<Result>;

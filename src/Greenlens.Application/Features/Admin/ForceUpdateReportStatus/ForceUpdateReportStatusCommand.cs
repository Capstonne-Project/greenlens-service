using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.ForceUpdateReportStatus;

/// <summary>Admin force-updates a report's status (e.g. escalate, fix data).</summary>
/// <remarks>Implements: BR-ADM-010 — audit logged via pipeline behavior.</remarks>
public sealed record ForceUpdateReportStatusCommand(
    Guid ReportId,
    ReportStatus NewStatus,
    string Reason) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "Report";
    string? IAuditable.AuditEntityId => ReportId.ToString();
}

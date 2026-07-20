using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ContentModeration.HideReport;

/// <summary>
/// Admin hides a report from public view (content moderation).
/// </summary>
/// <remarks>Implements: BR-ADM-006. Audit logged via IAuditable.</remarks>
public sealed record HideReportCommand(Guid ReportId, string Reason) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "Report";
    string? IAuditable.AuditEntityId => ReportId.ToString();
}

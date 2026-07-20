using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ContentModeration.UnhideReport;

/// <summary>
/// Admin unhides a previously hidden report.
/// </summary>
/// <remarks>Implements: BR-ADM-006. Audit logged via IAuditable.</remarks>
public sealed record UnhideReportCommand(Guid ReportId) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "Report";
    string? IAuditable.AuditEntityId => ReportId.ToString();
}

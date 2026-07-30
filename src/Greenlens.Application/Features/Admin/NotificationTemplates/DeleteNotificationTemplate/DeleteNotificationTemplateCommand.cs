using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.DeleteNotificationTemplate;

/// <summary>
/// Deactivate a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004, BR-ADM-010.</remarks>
public sealed record DeleteNotificationTemplateCommand(Guid Id) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "NotificationTemplate";
    string? IAuditable.AuditEntityId => Id.ToString();
}

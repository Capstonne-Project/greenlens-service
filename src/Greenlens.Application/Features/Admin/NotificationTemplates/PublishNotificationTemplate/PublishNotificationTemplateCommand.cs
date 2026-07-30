using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.PublishNotificationTemplate;

/// <summary>
/// Publish or unpublish a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004, BR-ADM-010.</remarks>
public sealed record PublishNotificationTemplateCommand(
    Guid Id, bool Publish = true) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "NotificationTemplate";
    string? IAuditable.AuditEntityId => Id.ToString();
}

using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.CreateNotificationTemplate;

/// <summary>
/// Create a new notification template with placeholders.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed record CreateNotificationTemplateCommand(
    string TemplateKey,
    string TitleVi,
    string BodyVi,
    string? TitleEn,
    string? BodyEn,
    NotificationChannel Channel,
    NotificationType Type) : IRequest<Result<CreateNotificationTemplateResponse>>;

public sealed record CreateNotificationTemplateResponse(Guid Id, string TemplateKey, bool IsPublished);

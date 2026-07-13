using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplateById;

public sealed record NotificationTemplateDetailResponse(
    Guid Id,
    string TemplateKey,
    string TitleVi,
    string BodyVi,
    string? TitleEn,
    string? BodyEn,
    NotificationChannel Channel,
    NotificationType Type,
    bool IsPublished,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

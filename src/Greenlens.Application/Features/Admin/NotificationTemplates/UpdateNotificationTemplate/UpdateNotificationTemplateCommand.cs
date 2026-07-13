using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.UpdateNotificationTemplate;

/// <summary>
/// Update a notification template.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed record UpdateNotificationTemplateCommand(
    Guid Id,
    string TitleVi,
    string BodyVi,
    string? TitleEn,
    string? BodyEn) : IRequest<Result>;

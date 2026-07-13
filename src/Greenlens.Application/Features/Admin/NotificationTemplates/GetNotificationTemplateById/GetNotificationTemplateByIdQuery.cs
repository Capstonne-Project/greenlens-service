using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplateById;

/// <summary>
/// Get a notification template by its ID.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed record GetNotificationTemplateByIdQuery(Guid Id) : IRequest<Result<NotificationTemplateDetailResponse>>;

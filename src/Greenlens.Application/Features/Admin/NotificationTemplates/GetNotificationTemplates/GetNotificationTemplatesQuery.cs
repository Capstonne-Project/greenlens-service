using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplates;

/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed record GetNotificationTemplatesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Channel = null,
    bool? IsPublished = null,
    bool? IsActive = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetNotificationTemplatesResponse>>;

public sealed record GetNotificationTemplatesResponse(
    List<NotificationTemplateItem> Items,
    int TotalCount);

public sealed record NotificationTemplateItem(
    Guid Id,
    string TemplateKey,
    string TitleVi,
    string Channel,
    string Type,
    bool IsPublished,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

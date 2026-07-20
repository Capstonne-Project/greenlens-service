using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.TestNotificationTemplate;

/// <summary>
/// Test-send a notification template with sample data before publishing.
/// Sends to the requesting admin's email/push — not to real users.
/// </summary>
/// <remarks>Implements: BR-ADM-004 (test gửi trước khi publish).</remarks>
public sealed record TestNotificationTemplateCommand(
    Guid TemplateId,
    /// <summary>Key-value pairs to fill placeholders, e.g. {"user_name":"Test User","report_id":"RPT-001"}.</summary>
    Dictionary<string, string> SampleData) : IRequest<Result<TestNotificationTemplateResponse>>;

public sealed record TestNotificationTemplateResponse(
    string RenderedTitle,
    string RenderedBody,
    string SentTo);

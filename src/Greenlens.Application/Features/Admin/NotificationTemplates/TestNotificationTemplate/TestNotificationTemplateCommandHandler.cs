using System.Text.RegularExpressions;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.NotificationTemplates.TestNotificationTemplate;

/// <summary>
/// Renders template with sample data and sends test notification to admin.
/// </summary>
/// <remarks>Implements: BR-ADM-004 (test gửi trước khi publish).</remarks>
public sealed class TestNotificationTemplateCommandHandler(
    INotificationTemplateRepository templates,
    ICurrentUser currentUser,
    INotificationService notificationService,
    ILogger<TestNotificationTemplateCommandHandler> logger)
    : IRequestHandler<TestNotificationTemplateCommand, Result<TestNotificationTemplateResponse>>
{
    private static readonly Regex PlaceholderPattern = new(@"\{[a-z_]+\}", RegexOptions.Compiled);

    public async Task<Result<TestNotificationTemplateResponse>> Handle(
        TestNotificationTemplateCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Testing notification template");

        var template = await templates.QueryAsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct)
            .ConfigureAwait(false);

        if (template is null)
        {
            logger.LogWarning("Notification template not found: {Id}", request.TemplateId);
            return Result<TestNotificationTemplateResponse>.Failure(Errors.Admin.NotificationTemplateNotFound);
        }
        logger.LogInformation("Notification template found: {Id}", request.TemplateId);
        // Render placeholders
        var renderedTitle = RenderPlaceholders(template.TitleVi, request.SampleData);
        var renderedBody = RenderPlaceholders(template.BodyVi, request.SampleData);
        logger.LogInformation("Rendered title: {RenderedTitle}", renderedTitle);
        logger.LogInformation("Rendered body: {RenderedBody}", renderedBody);
        // Send test notification to the requesting admin
        await notificationService.SendRawAsync(
            currentUser.UserId,
            template.Type,
            $"[TEST] {renderedTitle}",
            renderedBody,
            ct: ct).ConfigureAwait(false);

        logger.LogInformation("Test notification sent successfully to {Email}", currentUser.Email);

        return new TestNotificationTemplateResponse(
            renderedTitle,
            renderedBody,
            currentUser.Email);
    }

    private static string RenderPlaceholders(string template, Dictionary<string, string> data)
    {
        return PlaceholderPattern.Replace(template, match =>
        {
            var key = match.Value.Trim('{', '}');
            return data.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}

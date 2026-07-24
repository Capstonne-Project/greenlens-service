using System.Text.RegularExpressions;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.TestNotificationTemplate;

/// <summary>
/// Renders template with sample data and sends test notification to admin.
/// </summary>
/// <remarks>Implements: BR-ADM-004 (test gửi trước khi publish).</remarks>
public sealed class TestNotificationTemplateCommandHandler(
    INotificationTemplateRepository templates,
    ICurrentUser currentUser,
    INotificationService notificationService)
    : IRequestHandler<TestNotificationTemplateCommand, Result<TestNotificationTemplateResponse>>
{
    private static readonly Regex PlaceholderPattern = new(@"\{[a-z_]+\}", RegexOptions.Compiled);

    public async Task<Result<TestNotificationTemplateResponse>> Handle(
        TestNotificationTemplateCommand request,
        CancellationToken ct)
    {
        var template = await templates.QueryAsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct)
            .ConfigureAwait(false);

        if (template is null)
            return Result<TestNotificationTemplateResponse>.Failure(Errors.Admin.NotificationTemplateNotFound);

        // Render placeholders
        var renderedTitle = RenderPlaceholders(template.TitleVi, request.SampleData);
        var renderedBody = RenderPlaceholders(template.BodyVi, request.SampleData);

        // Send test notification to the requesting admin
        await notificationService.SendRawAsync(
            currentUser.UserId,
            template.Type,
            $"[TEST] {renderedTitle}",
            renderedBody,
            ct: ct).ConfigureAwait(false);

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

using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Admin-managed notification template with placeholders.
/// Templates must be published before use; admin can test-send before publishing.
/// </summary>
/// <remarks>Implements: BR-ADM-004.</remarks>
public sealed class NotificationTemplate : AuditableEntity
{
    private NotificationTemplate() { }

    /// <summary>Template key used by code to look up templates (e.g. "report_verified").</summary>
    public string TemplateKey { get; private set; } = default!;

    /// <summary>Vietnamese title with placeholders like {user_name}, {report_id}.</summary>
    public string TitleVi { get; private set; } = default!;

    /// <summary>Vietnamese body with placeholders.</summary>
    public string BodyVi { get; private set; } = default!;

    /// <summary>English title (optional).</summary>
    public string? TitleEn { get; private set; }

    /// <summary>English body (optional).</summary>
    public string? BodyEn { get; private set; }

    /// <summary>Notification channel this template is for.</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>Notification type this template maps to.</summary>
    public NotificationType Type { get; private set; }

    /// <summary>Only published templates are used by the system.</summary>
    public bool IsPublished { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static NotificationTemplate Create(
        string templateKey,
        string titleVi, string bodyVi,
        string? titleEn, string? bodyEn,
        NotificationChannel channel,
        NotificationType type)
    {
        return new NotificationTemplate
        {
            TemplateKey = templateKey,
            TitleVi = titleVi,
            BodyVi = bodyVi,
            TitleEn = titleEn,
            BodyEn = bodyEn,
            Channel = channel,
            Type = type,
            IsPublished = false,
            IsActive = true
        };
    }

    public void Update(string titleVi, string bodyVi, string? titleEn, string? bodyEn)
    {
        TitleVi = titleVi;
        BodyVi = bodyVi;
        TitleEn = titleEn;
        BodyEn = bodyEn;
        IsPublished = false; // Changes require re-publish
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

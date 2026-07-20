using System.Text.RegularExpressions;
using FluentValidation;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.CreateNotificationTemplate;

public sealed class CreateNotificationTemplateCommandValidator
    : AbstractValidator<CreateNotificationTemplateCommand>
{
    /// <summary>Allowed placeholders per BR-ADM-004.</summary>
    private static readonly HashSet<string> AllowedPlaceholders =
    [
        "{user_name}", "{report_id}", "{priority}", "{status}", "{time}",
        "{penalty_amount}", "{ward_name}", "{company_name}", "{report_code}",
        "{category_name}", "{severity}", "{officer_name}", "{team_name}"
    ];

    private static readonly Regex PlaceholderPattern = new(@"\{[a-z_]+\}", RegexOptions.Compiled);

    public CreateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateKey).NotEmpty().MaximumLength(100)
            .Matches("^[a-z][a-z0-9_]*$").WithMessage("TemplateKey phải snake_case (ví dụ: report_verified).");

        RuleFor(x => x.TitleVi).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BodyVi).NotEmpty().MaximumLength(4000)
            .Must(ContainOnlyValidPlaceholders).WithMessage("BodyVi chứa placeholder không hợp lệ.");

        RuleFor(x => x.TitleEn).MaximumLength(500).When(x => x.TitleEn is not null);
        RuleFor(x => x.BodyEn).MaximumLength(4000)
            .Must(ContainOnlyValidPlaceholders!)
            .When(x => x.BodyEn is not null)
            .WithMessage("BodyEn chứa placeholder không hợp lệ.");

        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
    }

    private static bool ContainOnlyValidPlaceholders(string text)
    {
        var matches = PlaceholderPattern.Matches(text);
        return matches.All(m => AllowedPlaceholders.Contains(m.Value));
    }
}

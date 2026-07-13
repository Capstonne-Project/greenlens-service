using FluentValidation;

namespace Greenlens.Application.Features.Admin.NotificationTemplates.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandValidator : AbstractValidator<UpdateNotificationTemplateCommand>
{
    public UpdateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        
        RuleFor(x => x.TitleVi)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.BodyVi)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.TitleEn)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.TitleEn));

        RuleFor(x => x.BodyEn)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.BodyEn));
    }
}

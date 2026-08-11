using FluentValidation;

namespace Greenlens.Application.Features.Admin.Badges.UpdateBadge;

public sealed class UpdateBadgeCommandValidator : AbstractValidator<UpdateBadgeCommand>
{
    public UpdateBadgeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameVi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.IconUrl).MaximumLength(500).When(x => x.IconUrl is not null);
    }
}

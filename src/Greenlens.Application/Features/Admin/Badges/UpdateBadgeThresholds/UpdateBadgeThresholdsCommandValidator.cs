using FluentValidation;

namespace Greenlens.Application.Features.Admin.Badges.UpdateBadgeThresholds;

public sealed class UpdateBadgeThresholdsCommandValidator : AbstractValidator<UpdateBadgeThresholdsCommand>
{
    public UpdateBadgeThresholdsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Threshold).InclusiveBetween(1, 1_000_000);
    }
}

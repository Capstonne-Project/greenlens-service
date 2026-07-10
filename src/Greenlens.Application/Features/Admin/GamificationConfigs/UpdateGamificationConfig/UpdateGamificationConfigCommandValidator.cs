using FluentValidation;

namespace Greenlens.Application.Features.Admin.GamificationConfigs.UpdateGamificationConfig;

public sealed class UpdateGamificationConfigCommandValidator
    : AbstractValidator<UpdateGamificationConfigCommand>
{
    public UpdateGamificationConfigCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Points).InclusiveBetween(-1000, 1000)
            .WithMessage("Điểm phải nằm trong khoảng -1000 đến 1000.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}

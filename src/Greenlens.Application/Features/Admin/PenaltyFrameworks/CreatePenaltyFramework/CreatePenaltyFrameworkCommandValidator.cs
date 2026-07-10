using FluentValidation;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.CreatePenaltyFramework;

public sealed class CreatePenaltyFrameworkCommandValidator : AbstractValidator<CreatePenaltyFrameworkCommand>
{
    public CreatePenaltyFrameworkCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ViolationLevel).IsInEnum();
        RuleFor(x => x.MinAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxAmount).GreaterThan(0);
        RuleFor(x => x.MaxAmount).GreaterThanOrEqualTo(x => x.MinAmount)
            .WithMessage("MaxAmount phải ≥ MinAmount.");
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.EffectiveTo).GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo phải sau EffectiveFrom.");
    }
}

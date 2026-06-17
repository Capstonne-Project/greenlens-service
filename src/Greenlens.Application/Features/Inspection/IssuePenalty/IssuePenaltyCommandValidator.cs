using FluentValidation;

namespace Greenlens.Application.Features.Inspection.IssuePenalty;

public sealed class IssuePenaltyCommandValidator : AbstractValidator<IssuePenaltyCommand>
{
    public IssuePenaltyCommandValidator()
    {
        RuleFor(x => x.InspectionId).NotEmpty();
        RuleFor(x => x.ViolationLevel).IsInEnum();
        RuleFor(x => x.PenaltyAmount).GreaterThan(0).WithMessage("Số tiền phạt phải lớn hơn 0.");
        RuleFor(x => x.DecisionNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PaymentDueDays).InclusiveBetween(1, 90);
        RuleFor(x => x.AdditionalMeasures).MaximumLength(1000).When(x => x.AdditionalMeasures is not null);
    }
}

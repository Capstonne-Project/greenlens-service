using FluentValidation;

namespace Greenlens.Application.Features.Inspection.DeclineInspection;

public sealed class DeclineInspectionCommandValidator : AbstractValidator<DeclineInspectionCommand>
{
    public DeclineInspectionCommandValidator()
    {
        RuleFor(x => x.InspectionId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .WithMessage("Lý do từ chối phải có ít nhất 10 ký tự.");
    }
}

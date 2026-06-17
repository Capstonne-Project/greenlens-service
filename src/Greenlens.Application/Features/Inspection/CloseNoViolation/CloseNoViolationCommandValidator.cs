using FluentValidation;

namespace Greenlens.Application.Features.Inspection.CloseNoViolation;

public sealed class CloseNoViolationCommandValidator : AbstractValidator<CloseNoViolationCommand>
{
    public CloseNoViolationCommandValidator()
    {
        RuleFor(x => x.InspectionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(50)
            .WithMessage("Lý do đóng hồ sơ phải có ít nhất 50 ký tự (BR-INS-013).");
    }
}

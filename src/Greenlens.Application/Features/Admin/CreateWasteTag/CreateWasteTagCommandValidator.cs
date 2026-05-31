using FluentValidation;

namespace Greenlens.Application.Features.Admin.CreateWasteTag;

public sealed class CreateWasteTagCommandValidator : AbstractValidator<CreateWasteTagCommand>
{
    public CreateWasteTagCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(50)
            .Matches("^[A-Z_]+$").WithMessage("Code phải viết HOA và dùng dấu gạch dưới.");

        RuleFor(x => x.NameVi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.IconUrl).MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

using FluentValidation;

namespace Greenlens.Application.Features.Admin.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code phải viết HOA, số, gạch dưới hoặc gạch ngang.");

        RuleFor(x => x.NameVi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.IconUrl).MaximumLength(500).When(x => x.IconUrl is not null);
    }
}

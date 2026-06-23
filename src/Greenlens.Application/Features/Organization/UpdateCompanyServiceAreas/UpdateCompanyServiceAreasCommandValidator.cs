using FluentValidation;

namespace Greenlens.Application.Features.Organization.UpdateCompanyServiceAreas;

public sealed class UpdateCompanyServiceAreasCommandValidator
    : AbstractValidator<UpdateCompanyServiceAreasCommand>
{
    public UpdateCompanyServiceAreasCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();

        RuleFor(x => x.WardCodes)
            .NotNull()
            .WithMessage("Danh sách wardCodes không được null.");

        RuleForEach(x => x.WardCodes)
            .NotEmpty()
            .WithMessage("WardCode không được rỗng.")
            .MaximumLength(20)
            .WithMessage("WardCode tối đa 20 ký tự.");

        RuleFor(x => x.WardCodes)
            .Must(codes => codes.Count == codes.Distinct().Count())
            .When(x => x.WardCodes is not null && x.WardCodes.Count > 0)
            .WithMessage("Danh sách wardCodes không được trùng lặp.");
    }
}

using FluentValidation;

namespace Greenlens.Application.Features.Catalog.GetProvinceBoundary;

public sealed class GetProvinceBoundaryQueryValidator : AbstractValidator<GetProvinceBoundaryQuery>
{
    public GetProvinceBoundaryQueryValidator()
    {
        RuleFor(x => x.ProvinceCode)
            .NotEmpty()
            .Length(2)
            .Matches(@"^\d{2}$")
            .WithMessage("ProvinceCode must be a 2-digit official code.");
    }
}

using FluentValidation;

namespace Greenlens.Application.Features.Catalog.GetWardsByProvince;

public sealed class GetWardsByProvinceQueryValidator : AbstractValidator<GetWardsByProvinceQuery>
{
    public GetWardsByProvinceQueryValidator()
    {
        RuleFor(x => x.ProvinceCode)
            .NotEmpty()
            .Length(2)
            .Matches(@"^\d{2}$")
            .WithMessage("ProvinceCode must be a 2-digit official code.");
    }
}

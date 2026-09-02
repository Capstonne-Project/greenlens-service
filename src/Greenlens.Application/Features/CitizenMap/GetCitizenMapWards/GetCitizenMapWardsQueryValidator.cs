using FluentValidation;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWards;

public sealed class GetCitizenMapWardsQueryValidator : AbstractValidator<GetCitizenMapWardsQuery>
{
    public GetCitizenMapWardsQueryValidator()
    {
        RuleFor(x => x.ProvinceCode)
            .NotEmpty()
            .Length(2)
            .Matches(@"^\d{2}$")
            .WithMessage("ProvinceCode must be a 2-digit official code.");
    }
}

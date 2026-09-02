using FluentValidation;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWardReports;

public sealed class GetCitizenMapWardReportsQueryValidator : AbstractValidator<GetCitizenMapWardReportsQuery>
{
    public GetCitizenMapWardReportsQueryValidator()
    {
        RuleFor(x => x.WardCode)
            .NotEmpty()
            .Length(5)
            .Matches(@"^\d{5}$")
            .WithMessage("WardCode must be a 5-digit official code.");
    }
}

using FluentValidation;

namespace Greenlens.Application.Features.Catalog.GetWardBoundary;

public sealed class GetWardBoundaryQueryValidator : AbstractValidator<GetWardBoundaryQuery>
{
    public GetWardBoundaryQueryValidator()
    {
        RuleFor(x => x.WardCode)
            .NotEmpty()
            .Length(5)
            .Matches(@"^\d{5}$")
            .WithMessage("WardCode must be a 5-digit official code.");
    }
}

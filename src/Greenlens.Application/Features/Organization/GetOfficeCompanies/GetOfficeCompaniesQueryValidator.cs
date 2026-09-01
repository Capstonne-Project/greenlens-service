using FluentValidation;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanies;

public sealed class GetOfficeCompaniesQueryValidator : AbstractValidator<GetOfficeCompaniesQuery>
{
    private const int MaxPageSize = 100;

    public GetOfficeCompaniesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));
        RuleFor(x => x.SortBy)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
    }
}

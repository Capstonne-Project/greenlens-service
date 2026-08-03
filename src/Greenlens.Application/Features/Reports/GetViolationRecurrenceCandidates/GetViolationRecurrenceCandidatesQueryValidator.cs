using FluentValidation;

namespace Greenlens.Application.Features.Reports.GetViolationRecurrenceCandidates;

public sealed class GetViolationRecurrenceCandidatesQueryValidator
    : AbstractValidator<GetViolationRecurrenceCandidatesQuery>
{
    public GetViolationRecurrenceCandidatesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

using FluentValidation;

namespace Greenlens.Application.Features.Reports.GetViolationRecurrenceCandidates;

public sealed class GetViolationRecurrenceCandidatesQueryValidator
    : AbstractValidator<GetViolationRecurrenceCandidatesQuery>
{
    public GetViolationRecurrenceCandidatesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        RuleFor(x => x.WardCode).MaximumLength(20).When(x => x.WardCode is not null);
        RuleFor(x => x.MinDaysSincePriorClosed)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinDaysSincePriorClosed.HasValue);
        RuleFor(x => x.MaxDaysSincePriorClosed)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxDaysSincePriorClosed.HasValue);
        RuleFor(x => x.MaxDaysSincePriorClosed)
            .GreaterThanOrEqualTo(x => x.MinDaysSincePriorClosed!.Value)
            .When(x => x.MinDaysSincePriorClosed.HasValue && x.MaxDaysSincePriorClosed.HasValue);
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}

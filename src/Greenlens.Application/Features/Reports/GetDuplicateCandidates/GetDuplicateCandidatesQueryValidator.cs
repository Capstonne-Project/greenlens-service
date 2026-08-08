using FluentValidation;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidates;

public sealed class GetDuplicateCandidatesQueryValidator
    : AbstractValidator<GetDuplicateCandidatesQuery>
{
    public GetDuplicateCandidatesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        RuleFor(x => x.WardCode).MaximumLength(20).When(x => x.WardCode is not null);
        RuleFor(x => x.DuplicateDetectionSource).MaximumLength(30).When(x => x.DuplicateDetectionSource is not null);
        RuleFor(x => x.MinAiSimilarityScore)
            .InclusiveBetween(0m, 1m)
            .When(x => x.MinAiSimilarityScore.HasValue);
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}

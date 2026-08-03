using FluentValidation;

namespace Greenlens.Application.Features.Inspection.GetOfficerInspectionQueue;

public sealed class GetOfficerInspectionQueueQueryValidator
    : AbstractValidator<GetOfficerInspectionQueueQuery>
{
    private const int MaxPageSize = 100;

    public GetOfficerInspectionQueueQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));
    }
}

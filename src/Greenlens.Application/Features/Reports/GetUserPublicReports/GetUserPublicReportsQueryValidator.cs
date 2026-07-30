using FluentValidation;

namespace Greenlens.Application.Features.Reports.GetUserPublicReports;

public sealed class GetUserPublicReportsQueryValidator
    : AbstractValidator<GetUserPublicReportsQuery>
{
    public GetUserPublicReportsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

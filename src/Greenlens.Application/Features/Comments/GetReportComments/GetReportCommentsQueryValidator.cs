using FluentValidation;

namespace Greenlens.Application.Features.Comments.GetReportComments;

public sealed class GetReportCommentsQueryValidator : AbstractValidator<GetReportCommentsQuery>
{
    public GetReportCommentsQueryValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

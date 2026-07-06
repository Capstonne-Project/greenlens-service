using FluentValidation;

namespace Greenlens.Application.Features.Reports.RateReport;

/// <summary>BR-REP-018: Validate rating range and comment length.</summary>
public sealed class RateReportCommandValidator : AbstractValidator<RateReportCommand>
{
    public RateReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .When(x => x.Rating.HasValue)
            .WithMessage("Đánh giá phải từ 1 đến 5 sao.");

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .When(x => x.Comment is not null)
            .WithMessage("Bình luận tối đa 500 ký tự.");
    }
}

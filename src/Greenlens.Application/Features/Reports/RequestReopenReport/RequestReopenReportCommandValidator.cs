using FluentValidation;

namespace Greenlens.Application.Features.Reports.RequestReopenReport;

/// <summary>Input validation for citizen reopen request (BR-REP-015, BR-REP-022).</summary>
public sealed class RequestReopenReportCommandValidator : AbstractValidator<RequestReopenReportCommand>
{
    public RequestReopenReportCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(2000);

        RuleFor(x => x.ImageUrls)
            .NotEmpty()
            .WithMessage("Cần ít nhất 1 ảnh minh chứng khi yêu cầu mở lại.");

        RuleFor(x => x.ImageUrls)
            .Must(urls => urls.Count <= 5)
            .When(x => x.ImageUrls is not null)
            .WithMessage("Tối đa 5 ảnh minh chứng.");
    }
}

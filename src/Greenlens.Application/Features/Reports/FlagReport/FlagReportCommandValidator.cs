using FluentValidation;

namespace Greenlens.Application.Features.Reports.FlagReport;

public sealed class FlagReportCommandValidator : AbstractValidator<FlagReportCommand>
{
    public FlagReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Reason)
            .MaximumLength(2000)
            .When(x => x.Reason is not null);
    }
}

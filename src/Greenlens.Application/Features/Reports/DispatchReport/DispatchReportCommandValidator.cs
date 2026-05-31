using FluentValidation;

namespace Greenlens.Application.Features.Reports.DispatchReport;

public sealed class DispatchReportCommandValidator : AbstractValidator<DispatchReportCommand>
{
    public DispatchReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.TargetLocalOfficeId).NotEmpty();
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null);
    }
}

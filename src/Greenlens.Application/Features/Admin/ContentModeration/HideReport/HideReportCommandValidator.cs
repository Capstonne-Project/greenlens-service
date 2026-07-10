using FluentValidation;

namespace Greenlens.Application.Features.Admin.ContentModeration.HideReport;

public sealed class HideReportCommandValidator : AbstractValidator<HideReportCommand>
{
    public HideReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

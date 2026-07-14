using FluentValidation;

namespace Greenlens.Application.Features.Reports.ConfirmDuplicate;

public sealed class ConfirmDuplicateCommandValidator : AbstractValidator<ConfirmDuplicateCommand>
{
    public ConfirmDuplicateCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.PrimaryReportId).NotEmpty();
    }
}

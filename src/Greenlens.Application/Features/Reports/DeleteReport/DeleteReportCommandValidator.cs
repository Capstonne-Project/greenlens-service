using FluentValidation;

namespace Greenlens.Application.Features.Reports.DeleteReport;

public sealed class DeleteReportCommandValidator : AbstractValidator<DeleteReportCommand>
{
    public DeleteReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}

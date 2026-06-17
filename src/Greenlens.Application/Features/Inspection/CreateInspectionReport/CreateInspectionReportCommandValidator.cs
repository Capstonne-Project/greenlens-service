using FluentValidation;

namespace Greenlens.Application.Features.Inspection.CreateInspectionReport;

public sealed class CreateInspectionReportCommandValidator : AbstractValidator<CreateInspectionReportCommand>
{
    public CreateInspectionReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ViolationDescription).MaximumLength(2000).When(x => x.ViolationDescription is not null);
        RuleFor(x => x.ViolatorName).MaximumLength(200).When(x => x.ViolatorName is not null);
        RuleFor(x => x.ViolatorAddress).MaximumLength(500).When(x => x.ViolatorAddress is not null);
        RuleFor(x => x.ViolatorIdentity).MaximumLength(50).When(x => x.ViolatorIdentity is not null);
    }
}

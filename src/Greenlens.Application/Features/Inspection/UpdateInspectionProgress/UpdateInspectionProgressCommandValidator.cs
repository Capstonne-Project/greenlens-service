using FluentValidation;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionProgress;

public sealed class UpdateInspectionProgressCommandValidator : AbstractValidator<UpdateInspectionProgressCommand>
{
    public UpdateInspectionProgressCommandValidator()
    {
        RuleFor(x => x.InspectionId).NotEmpty();
        RuleFor(x => x.Percent).InclusiveBetween(0, 100);
    }
}

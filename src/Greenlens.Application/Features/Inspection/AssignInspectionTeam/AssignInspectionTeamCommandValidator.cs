using FluentValidation;

namespace Greenlens.Application.Features.Inspection.AssignInspectionTeam;

public sealed class AssignInspectionTeamCommandValidator : AbstractValidator<AssignInspectionTeamCommand>
{
    public AssignInspectionTeamCommandValidator()
    {
        RuleFor(x => x.InspectionId)
            .NotEmpty().WithMessage("InspectionId không được để trống.");

        RuleFor(x => x.TeamId)
            .NotEmpty().WithMessage("TeamId không được để trống.");
    }
}

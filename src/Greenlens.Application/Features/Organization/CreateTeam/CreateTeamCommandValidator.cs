using FluentValidation;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization.CreateTeam;

public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên team là bắt buộc.")
            .MaximumLength(100);

        RuleFor(x => x.TeamType)
            .IsInEnum()
            .Must(t => t is TeamType.Cleanup or TeamType.Inspection)
            .WithMessage("TeamType phải là Cleanup hoặc Inspection.");
    }
}

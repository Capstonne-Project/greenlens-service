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

        When(x => x.TeamType == TeamType.Cleanup, () =>
        {
            RuleFor(x => x.WasteTagIds)
                .NotNull().WithMessage("Cleanup team phải có ít nhất một WasteTag.")
                .Must(ids => ids!.Count > 0).WithMessage("Cleanup team phải có ít nhất một WasteTag.");
        });

        When(x => x.TeamType == TeamType.Inspection, () =>
        {
            RuleFor(x => x.WasteTagIds)
                .Must(ids => ids is null || ids.Count == 0)
                .WithMessage("Inspection team không hỗ trợ WasteTag.");
        });
    }
}

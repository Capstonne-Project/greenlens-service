using FluentValidation;

namespace Greenlens.Application.Features.Reports.AssignTeam;

public sealed class AssignTeamCommandValidator : AbstractValidator<AssignTeamCommand>
{
    public AssignTeamCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();

        RuleFor(x => x.Teams)
            .NotEmpty().WithMessage("Phải chọn ít nhất một team.");

        RuleForEach(x => x.Teams).ChildRules(team =>
        {
            team.RuleFor(t => t.TeamId).NotEmpty();
            team.RuleFor(t => t.Note).MaximumLength(500).When(t => t.Note is not null);
        });

        When(x => x.WasteTagIds is { Count: > 0 }, () =>
        {
            RuleFor(x => x.WasteTagIds!)
                .Must(ids => ids.Count <= 10)
                .WithMessage("Tối đa 10 waste tags mỗi báo cáo.")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Danh sách waste tags không được trùng lặp.");
        });
    }
}

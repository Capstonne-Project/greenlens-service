using FluentValidation;

namespace Greenlens.Application.Features.Reports.VerifyReport;

public sealed class VerifyReportCommandValidator : AbstractValidator<VerifyReportCommand>
{
    public VerifyReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();

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

using FluentValidation;

namespace Greenlens.Application.Features.Reports.TagReportWaste;

public sealed class TagReportWasteCommandValidator : AbstractValidator<TagReportWasteCommand>
{
    public TagReportWasteCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();

        RuleFor(x => x.WasteTagIds)
            .NotNull()
            .Must(ids => ids.Count > 0).WithMessage("Phải chọn ít nhất 1 loại rác.")
            .Must(ids => ids.Count <= 12).WithMessage("Tối đa 12 loại rác.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Không được trùng tag.");
    }
}

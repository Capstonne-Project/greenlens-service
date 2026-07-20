using FluentValidation;
using Greenlens.Application.Common;

namespace Greenlens.Application.Features.Reports.AnalyzeReportImage;

public sealed class AnalyzeReportImageCommandValidator : AbstractValidator<AnalyzeReportImageCommand>
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB per AI Service contract

    public AnalyzeReportImageCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => ReportImageContentTypes.IsAllowed(x.FileName, x.ContentType))
            .WithMessage("Chỉ chấp nhận ảnh jpg, png, webp, heic.")
            .OverridePropertyName(nameof(AnalyzeReportImageCommand.ContentType));

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File không được rỗng.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("Ảnh vượt quá 20MB.");

        RuleFor(x => x.ImageBytes)
            .NotNull().NotEmpty().WithMessage("Dữ liệu ảnh không hợp lệ.");
    }
}

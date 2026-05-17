using FluentValidation;

namespace Greenlens.Application.Features.Reports.AnalyzeReportImage;

public sealed class AnalyzeReportImageCommandValidator : AbstractValidator<AnalyzeReportImageCommand>
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB per AI Service contract

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic"
    };

    public AnalyzeReportImageCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(t => AllowedContentTypes.Contains(t))
            .WithMessage("Chỉ chấp nhận ảnh jpg, png, webp, heic.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File không được rỗng.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("Ảnh vượt quá 20MB.");

        RuleFor(x => x.ImageBytes)
            .NotNull().NotEmpty().WithMessage("Dữ liệu ảnh không hợp lệ.");
    }
}

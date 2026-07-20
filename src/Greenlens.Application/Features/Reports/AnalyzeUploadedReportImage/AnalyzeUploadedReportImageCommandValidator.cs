using FluentValidation;
using Greenlens.Application.Common;

namespace Greenlens.Application.Features.Reports.AnalyzeUploadedReportImage;

public sealed class AnalyzeUploadedReportImageCommandValidator
    : AbstractValidator<AnalyzeUploadedReportImageCommand>
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;

    public AnalyzeUploadedReportImageCommandValidator()
    {
        RuleFor(x => x.PublicUrl)
            .NotEmpty()
            .MaximumLength(1_000);

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x)
            .Must(x => ReportImageContentTypes.IsAllowed(x.FileName, x.ContentType))
            .WithMessage("Chỉ chấp nhận ảnh jpg, png, webp, heic.")
            .OverridePropertyName(nameof(AnalyzeUploadedReportImageCommand.ContentType));

        RuleFor(x => x.SizeBytes)
            .InclusiveBetween(1, MaxImageSizeBytes)
            .WithMessage("Ảnh vượt quá 10MB.");
    }
}

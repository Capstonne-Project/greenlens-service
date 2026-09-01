using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Reports.AnalyzeUploadedReportImage;

public sealed class AnalyzeUploadedReportImageCommandValidator
    : AbstractValidator<AnalyzeUploadedReportImageCommand>
{
    public AnalyzeUploadedReportImageCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var maxImageSizeBytes = ReportSystemSettings.MaxImageSizeBytes(systemSettings);
        var maxImageSizeMb = ReportSystemSettings.MaxImageSizeMegabytes(systemSettings);

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
            .InclusiveBetween(1, maxImageSizeBytes)
            .WithMessage($"Ảnh vượt quá {maxImageSizeMb}MB.");
    }
}

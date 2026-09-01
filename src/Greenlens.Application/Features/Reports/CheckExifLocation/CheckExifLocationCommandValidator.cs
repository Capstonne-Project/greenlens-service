using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Reports.CheckExifLocation;

public sealed class CheckExifLocationCommandValidator : AbstractValidator<CheckExifLocationCommand>
{
    public CheckExifLocationCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var maxImageSizeBytes = ReportSystemSettings.MaxImageSizeBytes(systemSettings);
        var maxImageSizeMb = ReportSystemSettings.MaxImageSizeMegabytes(systemSettings);
        var (minLat, maxLat, minLng, maxLng) = ModuleSystemSettings.VietnamBounds(systemSettings);

        // BR-REP-003: Vietnam GPS bounds
        RuleFor(x => x.Latitude)
            .InclusiveBetween(minLat, maxLat)
            .WithMessage($"Latitude must be between {minLat} and {maxLat}.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(minLng, maxLng)
            .WithMessage($"Longitude must be between {minLng} and {maxLng}.");

        RuleFor(x => x)
            .Must(HasImageSource)
            .WithMessage("Phải cung cấp TempImageId hoặc thông tin ảnh R2 (publicUrl, key, fileName, contentType, sizeBytes).")
            .OverridePropertyName("ImageSource");

        When(x => !string.IsNullOrWhiteSpace(x.TempImageId), () =>
        {
            RuleFor(x => x.TempImageId!)
                .Length(32)
                .WithMessage("TempImageId không hợp lệ.");
        });

        When(x => string.IsNullOrWhiteSpace(x.TempImageId), () =>
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
                .Must(x => ReportImageContentTypes.IsAllowed(x.FileName!, x.ContentType!))
                .WithMessage("Chỉ chấp nhận ảnh jpg, png, webp, heic.")
                .OverridePropertyName(nameof(CheckExifLocationCommand.ContentType));

            RuleFor(x => x.SizeBytes)
                .NotNull()
                .InclusiveBetween(1, maxImageSizeBytes)
                .WithMessage($"Ảnh vượt quá {maxImageSizeMb}MB.");
        });
    }

    private static bool HasImageSource(CheckExifLocationCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.TempImageId))
            return true;

        return !string.IsNullOrWhiteSpace(command.PublicUrl)
               && !string.IsNullOrWhiteSpace(command.Key)
               && !string.IsNullOrWhiteSpace(command.FileName)
               && !string.IsNullOrWhiteSpace(command.ContentType)
               && command.SizeBytes is > 0;
    }
}

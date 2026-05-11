using FluentValidation;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

public sealed class SubmitPollutionReportCommandValidator : AbstractValidator<SubmitPollutionReportCommand>
{
    public const int MaxImagesPerReport = 5;
    public const long MaxImageSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic"
    };

    public SubmitPollutionReportCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Severity).IsInEnum();

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        // BR-REP-003: Vietnam GPS bounds (approximate)
        RuleFor(x => x.Latitude)
            .InclusiveBetween(8m, 24m)
            .WithMessage("Latitude must be between 8 and 24.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(102m, 110m)
            .WithMessage("Longitude must be between 102 and 110.");

        RuleFor(x => x.Address).MaximumLength(500);

        RuleFor(x => x.ProvinceCode)
            .MaximumLength(2)
            .Must(code => string.IsNullOrWhiteSpace(code) || IsTwoDigitProvinceCode(code))
            .WithMessage("ProvinceCode must be a 2-digit official code when provided.");

        RuleFor(x => x.WardCode)
            .MaximumLength(5)
            .Must(code => string.IsNullOrWhiteSpace(code) || IsFiveDigitWardCode(code))
            .WithMessage("WardCode must be a 5-digit official code when provided.");

        RuleFor(x => x)
            .Must(HavePairedOrEmptyAdministrativeCodes)
            .WithMessage("ProvinceCode and WardCode must both be set together or both omitted.");

        // BR-REP-001: at least one photo; BR-REP-002: max 5 images per report
        RuleFor(x => x.Images)
            .NotNull()
            .WithMessage("Images are required. Upload via POST /v1/media/reports/images first.")
            .Must(i => i.Count >= 1)
            .WithMessage("At least one image is required after upload.")
            .Must(i => i.Count <= MaxImagesPerReport)
            .WithMessage($"At most {MaxImagesPerReport} images per report.");

        RuleForEach(x => x.Images).ChildRules(img =>
        {
            img.RuleFor(i => i.Url)
                .NotEmpty()
                .MaximumLength(500)
                .Must(BeHttpsAbsoluteUri)
                .WithMessage("Each image url must be a valid absolute https URL.");

            img.RuleFor(i => i.MimeType)
                .NotEmpty()
                .Must(t => AllowedMimeTypes.Contains(t))
                .WithMessage("Mime type must be image/jpeg, image/png, image/webp, or image/heic.");

            img.RuleFor(i => i.SizeBytes)
                .InclusiveBetween(1, MaxImageSizeBytes)
                .WithMessage($"Image size must be between 1 and {MaxImageSizeBytes} bytes.");
        });
    }

    private static bool HavePairedOrEmptyAdministrativeCodes(SubmitPollutionReportCommand x)
    {
        var hasP = !string.IsNullOrWhiteSpace(x.ProvinceCode);
        var hasW = !string.IsNullOrWhiteSpace(x.WardCode);
        return hasP == hasW;
    }

    private static bool IsTwoDigitProvinceCode(string code) =>
        System.Text.RegularExpressions.Regex.IsMatch(code.Trim(), @"^\d{2}$");

    private static bool IsFiveDigitWardCode(string code) =>
        System.Text.RegularExpressions.Regex.IsMatch(code.Trim(), @"^\d{5}$");

    private static bool BeHttpsAbsoluteUri(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        return uri.Scheme == Uri.UriSchemeHttps;
    }
}

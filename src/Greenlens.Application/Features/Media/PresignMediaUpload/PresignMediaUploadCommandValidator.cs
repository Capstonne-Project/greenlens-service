using FluentValidation;
using Greenlens.Application.Common;

namespace Greenlens.Application.Features.Media.PresignMediaUpload;

public sealed class PresignMediaUploadCommandValidator : AbstractValidator<PresignMediaUploadCommand>
{
    public PresignMediaUploadCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.")
            .MaximumLength(200)
            .Must(name => !string.IsNullOrWhiteSpace(Path.GetFileName(name)))
            .WithMessage("FileName is invalid.");

        RuleFor(x => x.ContentType)
            .NotEmpty();

        RuleFor(x => x.Purpose)
            .IsInEnum();

        RuleFor(x => x.ReportId)
            .NotEmpty()
            .When(x => x.Purpose is MediaUploadPurpose.Before
                or MediaUploadPurpose.Progress
                or MediaUploadPurpose.ReopenEvidence)
            .WithMessage("ReportId is required for Before/Progress/ReopenEvidence uploads.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .When(x => x.FileSizeBytes.HasValue);
    }
}

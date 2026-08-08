using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Domain.Enums;

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

        RuleFor(x => x.InspectionId)
            .NotEmpty()
            .When(x => x.Purpose is MediaUploadPurpose.InspectionEvidence)
            .WithMessage("InspectionId is required for InspectionEvidence uploads.");

        RuleFor(x => x.EvidenceCategory)
            .NotNull()
            .Must(c => c is not InspectionEvidenceCategory.ViolationStatus)
            .When(x => x.Purpose is MediaUploadPurpose.InspectionEvidence)
            .WithMessage("EvidenceCategory is required and must not be ViolationStatus.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .When(x => x.FileSizeBytes.HasValue);
    }
}

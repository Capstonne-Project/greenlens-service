using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Inspection.UploadInspectionEvidence;

public sealed class UploadInspectionEvidenceCommandValidator
    : AbstractValidator<UploadInspectionEvidenceCommand>
{
    public UploadInspectionEvidenceCommandValidator()
    {
        RuleFor(x => x.Category)
            .Must(c => c is not InspectionEvidenceCategory.ViolationStatus)
            .WithMessage("ViolationStatus is text-only — use PUT /checklist.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one evidence item is required.");

        RuleFor(x => x.Items)
            .Must(items => items.Count <= InspectionEvidenceUploadRules.MaxItemsPerRequest)
            .WithMessage($"Maximum {InspectionEvidenceUploadRules.MaxItemsPerRequest} items per request.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Url)
                .NotEmpty()
                .MaximumLength(500);

            item.RuleFor(i => i.ContentType)
                .NotEmpty()
                .MaximumLength(100);

            item.RuleFor(i => i.SizeBytes)
                .GreaterThan(0);

            item.RuleFor(i => i.DurationSeconds)
                .GreaterThan(0)
                .When(i => i.DurationSeconds.HasValue);
        });

        RuleFor(x => x)
            .Custom((cmd, context) =>
            {
                var maxBytes = InspectionEvidenceUploadRules.MaxBytesFor(cmd.Category);
                for (var i = 0; i < cmd.Items.Count; i++)
                {
                    if (cmd.Items[i].SizeBytes > maxBytes)
                    {
                        context.AddFailure(
                            $"Items[{i}].SizeBytes",
                            $"File exceeds size limit for {cmd.Category}.");
                    }
                }
            });

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);
    }
}

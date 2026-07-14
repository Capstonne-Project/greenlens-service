using FluentValidation;

namespace Greenlens.Application.Features.Comments.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MinimumLength(1).MaximumLength(500);
        RuleFor(x => x.Images).Must(i => i is null || i.Count <= 2)
            .WithMessage("Maximum 2 images per comment.");
        RuleForEach(x => x.Images).ChildRules(img =>
        {
            img.RuleFor(i => i.Url).NotEmpty().MaximumLength(500);
            img.RuleFor(i => i.MimeType).NotEmpty().MaximumLength(50);
            img.RuleFor(i => i.SizeBytes).GreaterThan(0).LessThanOrEqualTo(5 * 1024 * 1024);
        }).When(x => x.Images is { Count: > 0 });
    }
}

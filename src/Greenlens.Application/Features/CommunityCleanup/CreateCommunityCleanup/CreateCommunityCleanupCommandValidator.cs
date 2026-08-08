using FluentValidation;

namespace Greenlens.Application.Features.CommunityCleanup.CreateCommunityCleanup;

public sealed class CreateCommunityCleanupCommandValidator : AbstractValidator<CreateCommunityCleanupCommand>
{
    public CreateCommunityCleanupCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.LeaderUserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.MeetingNote).MaximumLength(500);
        RuleFor(x => x.MaxParticipants).InclusiveBetween(1, 200);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).When(x => x.EndsAt.HasValue);
        RuleFor(x => x.JoinClosesAt).LessThanOrEqualTo(x => x.StartsAt).When(x => x.JoinClosesAt.HasValue);
    }
}

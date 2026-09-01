using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.CommunityCleanup.CheckInCommunityCleanup;

public sealed class CheckInCommunityCleanupCommandValidator : AbstractValidator<CheckInCommunityCleanupCommand>
{
    public CheckInCommunityCleanupCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var (minLat, maxLat, minLng, maxLng) = ModuleSystemSettings.VietnamBounds(systemSettings);
        var (rejectMin, _) = ModuleSystemSettings.ValidationReasonLengths(systemSettings);

        RuleFor(x => x.EventId).NotEmpty();

        // BR-REP-003: Vietnam GPS bounds
        RuleFor(x => x.Latitude)
            .InclusiveBetween(minLat, maxLat)
            .WithMessage($"Latitude phải trong khoảng {minLat}–{maxLat} (Việt Nam).");
        RuleFor(x => x.Longitude)
            .InclusiveBetween(minLng, maxLng)
            .WithMessage($"Longitude phải trong khoảng {minLng}–{maxLng} (Việt Nam).");

        // Draft: out-of-range check-in override reason, when supplied, must be meaningful.
        RuleFor(x => x.Reason)
            .MinimumLength(rejectMin)
            .WithMessage($"Lý do phải có ít nhất {rejectMin} ký tự.")
            .When(x => x.Reason is not null);
    }
}

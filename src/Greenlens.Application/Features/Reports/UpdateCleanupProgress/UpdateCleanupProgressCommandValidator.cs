using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Reports.UpdateCleanupProgress;

public sealed class UpdateCleanupProgressCommandValidator : AbstractValidator<UpdateCleanupProgressCommand>
{
    public UpdateCleanupProgressCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var (minLat, maxLat, minLng, maxLng) = ModuleSystemSettings.VietnamBounds(systemSettings);

        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Percent).InclusiveBetween(0, 100);

        // BR-REP-003: Vietnam GPS bounds
        RuleFor(x => x.Latitude)
            .InclusiveBetween(minLat, maxLat)
            .WithMessage($"Latitude phải trong khoảng {minLat}–{maxLat} (Việt Nam).");
        RuleFor(x => x.Longitude)
            .InclusiveBetween(minLng, maxLng)
            .WithMessage($"Longitude phải trong khoảng {minLng}–{maxLng} (Việt Nam).");
    }
}

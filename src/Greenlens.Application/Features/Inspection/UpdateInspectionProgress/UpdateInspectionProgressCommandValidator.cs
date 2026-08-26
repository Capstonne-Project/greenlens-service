using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionProgress;

public sealed class UpdateInspectionProgressCommandValidator : AbstractValidator<UpdateInspectionProgressCommand>
{
    public UpdateInspectionProgressCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var (minLat, maxLat, minLng, maxLng) = ModuleSystemSettings.VietnamBounds(systemSettings);

        RuleFor(x => x.InspectionId).NotEmpty();
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

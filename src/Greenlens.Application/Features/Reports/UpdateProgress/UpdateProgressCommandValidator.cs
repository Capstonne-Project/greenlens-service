using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Reports.Common;

namespace Greenlens.Application.Features.Reports.UpdateProgress;

public sealed class UpdateProgressCommandValidator : AbstractValidator<UpdateProgressCommand>
{
    public UpdateProgressCommandValidator(ISystemSettingsProvider systemSettings)
    {
        var (minLat, maxLat, minLng, maxLng) = ModuleSystemSettings.VietnamBounds(systemSettings);

        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ProgressPercent).InclusiveBetween(0, 100);

        // BR-REP-003: Vietnam GPS bounds
        RuleFor(x => x.Latitude)
            .Must((cmd, lat) => ProgressUpdateCoordinates.IsMissing(lat, cmd.Longitude)
                || (lat >= minLat && lat <= maxLat))
            .WithMessage($"Latitude phải trong khoảng {minLat}–{maxLat} (Việt Nam), hoặc 0 khi chưa có GPS ảnh.");
        RuleFor(x => x.Longitude)
            .Must((cmd, lng) => ProgressUpdateCoordinates.IsMissing(cmd.Latitude, lng)
                || (lng >= minLng && lng <= maxLng))
            .WithMessage($"Longitude phải trong khoảng {minLng}–{maxLng} (Việt Nam), hoặc 0 khi chưa có GPS ảnh.");
    }
}

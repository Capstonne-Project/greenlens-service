using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Map;

namespace Greenlens.Application.Features.Map.GetMapViewportSummary;

public sealed class GetMapViewportSummaryQueryValidator : AbstractValidator<GetMapViewportSummaryQuery>
{
    public GetMapViewportSummaryQueryValidator(ISystemSettingsProvider systemSettings)
    {
        var (minLat, maxLat, minLng, maxLng) = ModuleSystemSettings.VietnamBounds(systemSettings);
        var (maxLatSpan, maxLngSpan) = ModuleSystemSettings.MapBoundingSpans(systemSettings);
        var (_, minDays, maxDays) = ModuleSystemSettings.MapViewportDays(systemSettings);

        RuleFor(x => x.MinLat)
            .InclusiveBetween(minLat, maxLat);

        RuleFor(x => x.MaxLat)
            .InclusiveBetween(minLat, maxLat);

        RuleFor(x => x.MinLng)
            .InclusiveBetween(minLng, maxLng);

        RuleFor(x => x.MaxLng)
            .InclusiveBetween(minLng, maxLng);

        RuleFor(x => x)
            .Must(q => q.MinLat < q.MaxLat && q.MinLng < q.MaxLng)
            .WithMessage("minLat must be less than maxLat and minLng less than maxLng.");

        RuleFor(x => x)
            .Must(q =>
                q.MaxLat - q.MinLat <= maxLatSpan &&
                q.MaxLng - q.MinLng <= maxLngSpan)
            .WithMessage("Bounding box is too large; zoom in.");

        RuleFor(x => x.Days)
            .InclusiveBetween(minDays, maxDays);
    }
}

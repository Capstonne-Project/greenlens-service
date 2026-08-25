using FluentValidation;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Map;

namespace Greenlens.Application.Features.Map.GetPublicMapReports;

public sealed class GetPublicMapReportsQueryValidator : AbstractValidator<GetPublicMapReportsQuery>
{
    private static readonly string[] AllowedModes = ["detail", "aggregate"];

    public GetPublicMapReportsQueryValidator(ISystemSettingsProvider systemSettings)
    {
        var (minLat, maxLat, minLng, maxLng) = ModuleSystemSettings.VietnamBounds(systemSettings);
        var (maxLatSpan, maxLngSpan) = ModuleSystemSettings.MapBoundingSpans(systemSettings);
        var (_, maxDetailLimit) = ModuleSystemSettings.MapDetailLimits(systemSettings);

        RuleFor(x => x.Mode)
            .NotEmpty()
            .Must(m => AllowedModes.Contains(m.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("mode must be detail or aggregate.");

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

        RuleFor(x => x.Limit)
            .Must(l => !l.HasValue ||
                       (l.Value >= 1 && l.Value <= maxDetailLimit))
            .When(x => string.Equals(x.Mode, "detail", StringComparison.OrdinalIgnoreCase))
            .WithMessage($"limit must be between 1 and {maxDetailLimit} when provided.");

        RuleFor(x => x.GridLevel)
            .InclusiveBetween(PublicMapQueryLimits.MinGridLevel, PublicMapQueryLimits.MaxGridLevel)
            .When(x =>
                string.Equals(x.Mode, "aggregate", StringComparison.OrdinalIgnoreCase) &&
                x.GridLevel.HasValue)
            .WithMessage(
                $"gridLevel must be between {PublicMapQueryLimits.MinGridLevel} and {PublicMapQueryLimits.MaxGridLevel}.");
    }
}

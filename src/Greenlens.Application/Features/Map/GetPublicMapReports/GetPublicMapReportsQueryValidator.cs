using FluentValidation;
using Greenlens.Application.Common.Map;

namespace Greenlens.Application.Features.Map.GetPublicMapReports;

public sealed class GetPublicMapReportsQueryValidator : AbstractValidator<GetPublicMapReportsQuery>
{
    private static readonly string[] AllowedModes = ["detail", "aggregate"];

    public GetPublicMapReportsQueryValidator()
    {
        RuleFor(x => x.Mode)
            .NotEmpty()
            .Must(m => AllowedModes.Contains(m.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("mode must be detail or aggregate.");

        RuleFor(x => x.MinLat)
            .InclusiveBetween(PublicMapQueryLimits.MinLatitudeVn, PublicMapQueryLimits.MaxLatitudeVn);

        RuleFor(x => x.MaxLat)
            .InclusiveBetween(PublicMapQueryLimits.MinLatitudeVn, PublicMapQueryLimits.MaxLatitudeVn);

        RuleFor(x => x.MinLng)
            .InclusiveBetween(PublicMapQueryLimits.MinLongitudeVn, PublicMapQueryLimits.MaxLongitudeVn);

        RuleFor(x => x.MaxLng)
            .InclusiveBetween(PublicMapQueryLimits.MinLongitudeVn, PublicMapQueryLimits.MaxLongitudeVn);

        RuleFor(x => x)
            .Must(q => q.MinLat < q.MaxLat && q.MinLng < q.MaxLng)
            .WithMessage("minLat must be less than maxLat and minLng less than maxLng.");

        RuleFor(x => x)
            .Must(q =>
                q.MaxLat - q.MinLat <= PublicMapQueryLimits.MaxBoundingLatSpan &&
                q.MaxLng - q.MinLng <= PublicMapQueryLimits.MaxBoundingLngSpan)
            .WithMessage("Bounding box is too large; zoom in.");

        RuleFor(x => x.Limit)
            .Must(l => !l.HasValue ||
                       (l.Value >= 1 && l.Value <= PublicMapQueryLimits.MaxDetailLimit))
            .When(x => string.Equals(x.Mode, "detail", StringComparison.OrdinalIgnoreCase))
            .WithMessage($"limit must be between 1 and {PublicMapQueryLimits.MaxDetailLimit} when provided.");

        RuleFor(x => x.GridLevel)
            .InclusiveBetween(PublicMapQueryLimits.MinGridLevel, PublicMapQueryLimits.MaxGridLevel)
            .When(x =>
                string.Equals(x.Mode, "aggregate", StringComparison.OrdinalIgnoreCase) &&
                x.GridLevel.HasValue)
            .WithMessage(
                $"gridLevel must be between {PublicMapQueryLimits.MinGridLevel} and {PublicMapQueryLimits.MaxGridLevel}.");
    }
}

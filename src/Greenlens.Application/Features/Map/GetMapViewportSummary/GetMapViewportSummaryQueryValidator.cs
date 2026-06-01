using FluentValidation;
using Greenlens.Application.Common.Map;

namespace Greenlens.Application.Features.Map.GetMapViewportSummary;

public sealed class GetMapViewportSummaryQueryValidator : AbstractValidator<GetMapViewportSummaryQuery>
{
    public GetMapViewportSummaryQueryValidator()
    {
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

        RuleFor(x => x.Days)
            .InclusiveBetween(
                PublicMapViewportSummaryLimits.MinDays,
                PublicMapViewportSummaryLimits.MaxDays);
    }
}

using System.Globalization;
using System.Text.Json;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Reports;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace Greenlens.Infrastructure.Imaging;

/// <summary>BR-REP-011: reads EXIF DateTimeOriginal and GPS from uploaded report images.</summary>
public sealed class MetadataExtractorImageExifAnalyzer : IImageExifAnalyzer
{
    public ImageExifAnalysis Analyze(ReadOnlyMemory<byte> imageBytes, DateTime submittedAtUtc)
    {
        if (imageBytes.IsEmpty)
            return MissingAnalysis(submittedAtUtc);

        try
        {
            using var stream = new MemoryStream(imageBytes.ToArray(), writable: false);
            var directories = ImageMetadataReader.ReadMetadata(stream);

            var exifSub = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var gps = directories.OfType<GpsDirectory>().FirstOrDefault();

            DateTime? capturedAtUtc = null;
            if (exifSub is not null)
            {
                if (exifSub.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var original))
                    capturedAtUtc = DateTime.SpecifyKind(original, DateTimeKind.Utc);
                else if (exifSub.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var fallback))
                    capturedAtUtc = DateTime.SpecifyKind(fallback, DateTimeKind.Utc);
            }
            else if (exifIfd0 is not null)
            {
                if (exifIfd0.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var original))
                    capturedAtUtc = DateTime.SpecifyKind(original, DateTimeKind.Utc);
                else if (exifIfd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var fallback))
                    capturedAtUtc = DateTime.SpecifyKind(fallback, DateTimeKind.Utc);
            }

            decimal? latitude = null;
            decimal? longitude = null;
            if (gps is not null)
            {
                var geo = gps.GetGeoLocation();
                if (geo is not null)
                {
                    latitude = (decimal)geo.Latitude;
                    longitude = (decimal)geo.Longitude;
                }
            }

            var hasTimestamp = capturedAtUtc.HasValue;
            var reason = ExifSuspicionEvaluator.ResolveSuspiciousReason(
                hasTimestamp, capturedAtUtc, submittedAtUtc);

            string? exifJson = null;
            if (hasTimestamp || latitude.HasValue)
            {
                exifJson = JsonSerializer.Serialize(new
                {
                    dateTakenUtc = capturedAtUtc?.ToString("O", CultureInfo.InvariantCulture),
                    latitude,
                    longitude
                });
            }

            return new ImageExifAnalysis(
                hasTimestamp,
                capturedAtUtc,
                latitude,
                longitude,
                exifJson,
                reason is not null,
                reason);
        }
        catch
        {
            return MissingAnalysis(submittedAtUtc);
        }
    }

    private static ImageExifAnalysis MissingAnalysis(DateTime submittedAtUtc)
    {
        var reason = ExifSuspicionEvaluator.ResolveSuspiciousReason(false, null, submittedAtUtc);
        return new ImageExifAnalysis(false, null, null, null, null, true, reason);
    }
}

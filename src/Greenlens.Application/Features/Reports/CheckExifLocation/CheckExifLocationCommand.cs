using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.CheckExifLocation;

/// <summary>
/// Compare map-selected GPS with EXIF GPS embedded in a report image before submit.
/// </summary>
public sealed record CheckExifLocationCommand(
    decimal Latitude,
    decimal Longitude,

    /// <summary>Optional temp ID from analyze / analyze-uploaded (TTL 15 minutes).</summary>
    string? TempImageId,

    /// <summary>R2 direct-upload metadata — used when TempImageId is not available.</summary>
    string? PublicUrl,
    string? Key,
    string? FileName,
    string? ContentType,
    long? SizeBytes) : IRequest<Result<CheckExifLocationResponse>>, INoTransaction;

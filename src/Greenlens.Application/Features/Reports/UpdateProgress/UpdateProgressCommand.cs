using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.UpdateProgress;

/// <summary>
/// Team leader updates progress (%, note, optional image URLs already on R2).
/// </summary>
public sealed record UpdateProgressCommand(
    Guid ReportId,
    int ProgressPercent,
    string? ProgressNote,
    IReadOnlyList<string> ImageUrls,
    decimal Latitude,
    decimal Longitude) : IRequest<Result<UpdateProgressResponse>>;

public sealed record UpdateProgressResponse(IReadOnlyList<string> UploadedImageUrls);

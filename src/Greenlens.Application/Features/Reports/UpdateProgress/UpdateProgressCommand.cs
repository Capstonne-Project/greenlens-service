using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.UpdateProgress;

public sealed record ProgressImageFile(byte[] Bytes, string FileName, string ContentType);

/// <summary>
/// Team leader updates progress (%, note, optional images). TeamId resolved from token.
/// </summary>
public sealed record UpdateProgressCommand(
    Guid ReportId,
    int ProgressPercent,
    string? ProgressNote,
    IReadOnlyList<ProgressImageFile> Images) : IRequest<Result<UpdateProgressResponse>>;

public sealed record UpdateProgressResponse(IReadOnlyList<string> UploadedImageUrls);

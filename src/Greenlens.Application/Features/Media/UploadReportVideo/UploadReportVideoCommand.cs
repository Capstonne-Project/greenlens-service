using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Media.UploadReportVideo;

/// <summary>
/// Upload a report video to R2 Cloudflare (folder: reports/videos).
/// Video is transcoded server-side (H.264, 720p, CRF 28) before storage.
/// </summary>
/// <remarks>Implements: BR-REP-002 (video 1, mp4/mov).</remarks>
public sealed record UploadReportVideoCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<UploadReportVideoResponse>>;

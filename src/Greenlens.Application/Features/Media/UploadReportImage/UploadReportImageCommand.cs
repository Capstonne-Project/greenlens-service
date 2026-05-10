using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Media.UploadReportImage;

/// <summary>
/// Upload a report image to R2 Cloudflare (folder: reports/images).
/// </summary>
public sealed record UploadReportImageCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<UploadReportImageResponse>>;

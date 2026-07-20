using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.UploadBeforeImages;

/// <summary>
/// BR-REP-014: Persist before-image public URLs after client direct-to-R2 upload.
/// </summary>
public sealed record UploadBeforeImagesCommand(
    Guid ReportId,
    IReadOnlyList<string> ImageUrls) : IRequest<Result<UploadBeforeImagesResponse>>;

public sealed record UploadBeforeImagesResponse(IReadOnlyList<string> UploadedImageUrls);

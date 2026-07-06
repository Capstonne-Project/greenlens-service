using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.UploadBeforeImages;

/// <summary>
/// BR-REP-014: Upload before images after check-in, before starting cleanup work.
/// Team leader uploads photos of the current state at the report location.
/// </summary>
public sealed record BeforeImageFile(byte[] Bytes, string FileName, string ContentType);

public sealed record UploadBeforeImagesCommand(
    Guid ReportId,
    IReadOnlyList<BeforeImageFile> Images) : IRequest<Result<UploadBeforeImagesResponse>>;

public sealed record UploadBeforeImagesResponse(IReadOnlyList<string> UploadedImageUrls);

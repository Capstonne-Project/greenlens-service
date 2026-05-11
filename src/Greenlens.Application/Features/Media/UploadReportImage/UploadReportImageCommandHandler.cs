using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Media.UploadReportImage;

/// <summary>
/// Upload report image to R2 Cloudflare under reports/images folder.
/// </summary>
public sealed class UploadReportImageCommandHandler(
    IFileStorageService fileStorage,
    ILogger<UploadReportImageCommandHandler> logger)
    : IRequestHandler<UploadReportImageCommand, Result<UploadReportImageResponse>>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic"
    };

    public async Task<Result<UploadReportImageResponse>> Handle(
        UploadReportImageCommand request,
        CancellationToken cancellationToken)
    {
        // ── Validate ──
        if (!AllowedContentTypes.Contains(request.ContentType))
            return Errors.Media.InvalidImageType;

        if (request.FileSize > MaxFileSizeBytes)
            return Errors.Media.ImageTooLarge;

        // ── Upload to R2 ──
        FileUploadResult uploadResult;
        try
        {
            uploadResult = await fileStorage.UploadAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                "reports/images",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload report image to R2");
            return Errors.Users.StorageUploadFailed;
        }

        return new UploadReportImageResponse(
            uploadResult.Url,
            uploadResult.Key,
            "Tải ảnh báo cáo thành công.",
            request.ContentType,
            request.FileSize);
    }
}

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

    public async Task<Result<UploadReportImageResponse>> Handle(
        UploadReportImageCommand request,
        CancellationToken cancellationToken)
    {
        // ── Validate MIME (extension fallback for HEIC/octet-stream from Swagger) ──
        if (!ReportImageContentTypes.TryResolve(request.FileName, request.ContentType, out var contentType))
        {
            logger.LogWarning(
                "Rejected report image upload: fileName={FileName}, contentType={ContentType}",
                request.FileName, request.ContentType);
            return Errors.Media.InvalidImageType;
        }

        if (request.FileSize > MaxFileSizeBytes)
        {
            logger.LogWarning("Report image too large {Size} bytes", request.FileSize);
            return Errors.Media.ImageTooLarge;
        }

        // ── Upload to R2 ──
        FileUploadResult uploadResult;
        try
        {
            uploadResult = await fileStorage.UploadAsync(
                request.FileStream,
                request.FileName,
                contentType,
                "reports/images",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload report image to R2");
            return Errors.Users.StorageUploadFailed;
        }

        logger.LogInformation("Uploaded report image {FileName} to R2", request.FileName);

        return new UploadReportImageResponse(
            uploadResult.Url,
            uploadResult.Key,
            "Tải ảnh báo cáo thành công.",
            contentType,
            request.FileSize);
    }
}

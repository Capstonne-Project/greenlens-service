using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Media.PresignMediaUpload;
using Greenlens.Domain.Enums;
using Greenlens.Application.Features.Media.UploadCommentImage;
using Greenlens.Application.Features.Media.UploadReportImage;
using Greenlens.Application.Features.Media.UploadReportVideo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/media")]
[Authorize]
[Produces("application/json")]
[Tags("📎 Media — File Upload")]
public sealed class MediaController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Preferred upload path: Mobile uploads directly to R2 via short-lived presigned PUT URL.
    /// </summary>
    [HttpPost("presign")]
    [SwaggerOperation(
        Summary = "Presign direct R2 upload (preferred)",
        Description =
            "Returns a short-lived PUT URL so the client uploads the file directly to Cloudflare R2. " +
            "Flow: 1) POST /media/presign 2) PUT binary to uploadUrl with requiredHeaders 3) send publicUrl to report APIs. " +
            "Purpose: ReportImage|Before|Progress|After|Comment|Avatar|InspectionEvidence. " +
            "Before/Progress/ReopenEvidence require reportId. InspectionEvidence requires inspectionId + evidenceCategory.")]
    [SwaggerResponse(200, "Presigned URL created", typeof(ApiResponse<PresignMediaUploadResponse>))]
    [SwaggerResponse(400, "Invalid MIME / purpose / filename", typeof(ApiResponse))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(500, "Could not create presigned URL", typeof(ApiResponse))]
    public async Task<IActionResult> PresignAsync(
        [FromBody] PresignMediaRequest body,
        CancellationToken ct)
        => (await sender.Send(new PresignMediaUploadCommand(
            body.FileName,
            body.ContentType,
            body.Purpose,
            body.ReportId,
            body.InspectionId,
            body.EvidenceCategory,
            body.FileSizeBytes), ct)).ToHttp();

    [HttpPost("reports/images")]
    [SwaggerOperation(
        Summary = "[Deprecated] Upload Report Image via BE proxy",
        Description =
            "DEPRECATED — prefer POST /v1/media/presign then PUT to R2. " +
            "Legacy multipart upload through API (jpg/png/webp/heic, max 10MB). Kept for rollback.")]
    [SwaggerResponse(200, "Image uploaded", typeof(ApiResponse<UploadReportImageResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid image type or too large", typeof(ApiResponse))]
    [SwaggerResponse(500, "Storage upload failed", typeof(ApiResponse))]
    [Consumes("multipart/form-data")]
    [Obsolete("Use POST /v1/media/presign + direct R2 PUT instead.")]
    public async Task<IActionResult> UploadReportImageAsync(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponse
            {
                Code = "FILE_REQUIRED",
                Message = "Vui lòng chọn file ảnh.",
                Status = 400
            });

        await using var stream = file.OpenReadStream();

        // Strip path segments — some clients send full local path as FileName.
        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "upload";

        var command = new UploadReportImageCommand(
            stream,
            fileName,
            file.ContentType,
            file.Length);

        return (await sender.Send(command, ct)).ToHttp();
    }

    [HttpPost("comments/images")]
    [SwaggerOperation(
        Summary = "Upload Comment Image (BR-CMT-002)",
        Description = "Upload ảnh đính kèm bình luận (jpg/png/webp/heic, max 5MB). " +
            "Dùng url/mimeType/sizeBytes trong POST /v1/reports/{id}/comments.")]
    [SwaggerResponse(200, "Image uploaded", typeof(ApiResponse<UploadCommentImageResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid image type or too large", typeof(ApiResponse))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCommentImageAsync(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponse
            {
                Code = "FILE_REQUIRED",
                Message = "Vui lòng chọn file ảnh.",
                Status = 400
            });

        await using var stream = file.OpenReadStream();

        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "upload";

        var command = new UploadCommentImageCommand(
            stream,
            fileName,
            file.ContentType,
            file.Length);

        return (await sender.Send(command, ct)).ToHttp();
    }

    /// <summary>
    /// Upload a video for a pollution report (mp4/mov, max 100MB, max 60s).
    /// Video is compressed server-side (H.264 720p CRF 28) before storage on R2.
    /// </summary>
    /// <remarks>Implements: BR-REP-002.</remarks>
    [HttpPost("reports/videos")]
    [RequestSizeLimit(100_000_000)] // 100MB
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
    [SwaggerOperation(
        Summary = "Upload Report Video (BR-REP-002)",
        Description =
            "Upload a video for a pollution report (mp4/mov, max 100MB input, max 60s). " +
            "Video is automatically compressed server-side (H.264, 720p, CRF 28, AAC 96k) " +
            "similar to Messenger/Zalo compression. " +
            "Response includes both original and compressed file sizes.")]
    [SwaggerResponse(200, "Video uploaded and compressed", typeof(ApiResponse<UploadReportVideoResponse>))]
    [SwaggerResponse(400, "No file provided", typeof(ApiResponse))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid video type, too large, or too long", typeof(ApiResponse))]
    [SwaggerResponse(500, "Transcode or storage failed", typeof(ApiResponse))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadReportVideoAsync(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponse
            {
                Code = "FILE_REQUIRED",
                Message = "Vui lòng chọn file video.",
                Status = 400
            });

        await using var stream = file.OpenReadStream();

        var command = new UploadReportVideoCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        return (await sender.Send(command, ct)).ToHttp();
    }
}

public sealed record PresignMediaRequest(
    string FileName,
    string ContentType,
    MediaUploadPurpose Purpose,
    Guid? ReportId = null,
    Guid? InspectionId = null,
    InspectionEvidenceCategory? EvidenceCategory = null,
    long? FileSizeBytes = null);


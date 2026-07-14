using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Media.UploadCommentImage;

public sealed record UploadCommentImageCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<UploadCommentImageResponse>>;

public sealed record UploadCommentImageResponse(
    string Url,
    string Key,
    string Message,
    string MimeType,
    long SizeBytes);

/// <summary>Upload comment attachment image (max 5MB). BR-CMT-002.</summary>
public sealed class UploadCommentImageCommandHandler(
    IFileStorageService fileStorage,
    ILogger<UploadCommentImageCommandHandler> logger)
    : IRequestHandler<UploadCommentImageCommand, Result<UploadCommentImageResponse>>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public async Task<Result<UploadCommentImageResponse>> Handle(
        UploadCommentImageCommand request,
        CancellationToken ct)
    {
        if (!ReportImageContentTypes.TryResolve(request.FileName, request.ContentType, out var contentType))
            return Errors.Media.InvalidImageType;

        if (request.FileSize > MaxFileSizeBytes)
            return Errors.Comments.CommentImageTooLarge;

        try
        {
            var upload = await fileStorage.UploadAsync(
                request.FileStream, request.FileName, contentType,
                "comments/images", ct).ConfigureAwait(false);

            return new UploadCommentImageResponse(
                upload.Url, upload.Key,
                "Tải ảnh bình luận thành công.",
                contentType, request.FileSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload comment image");
            return Errors.Users.StorageUploadFailed;
        }
    }
}

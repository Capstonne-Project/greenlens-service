using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.UploadUserAvatar;

/// <summary>
/// Upload avatar to R2 Cloudflare and update user's AvatarUrl.
/// </summary>
public sealed class UploadUserAvatarCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IFileStorageService fileStorage,
    ILogger<UploadUserAvatarCommandHandler> logger)
    : IRequestHandler<UploadUserAvatarCommand, Result<UploadUserAvatarResponse>>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<Result<UploadUserAvatarResponse>> Handle(
        UploadUserAvatarCommand request,
        CancellationToken cancellationToken)
    {
        // ── Validate file ──
        if (!AllowedContentTypes.Contains(request.ContentType))
            return Errors.Users.InvalidFileType;

        if (request.FileSize > MaxFileSizeBytes)
            return Errors.Users.FileTooLarge;

        // ── Load user ──
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Users.UserNotFound;

        // ── Upload to R2 ──
        FileUploadResult uploadResult;
        try
        {
            uploadResult = await fileStorage.UploadAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                "avatars",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload avatar for user {UserId} to R2", currentUser.UserId);
            return Errors.Users.StorageUploadFailed;
        }

        // ── Update user entity ──
        user.UpdateProfile(avatarUrl: uploadResult.Url);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UploadUserAvatarResponse(uploadResult.Url, "Cập nhật ảnh đại diện thành công.");
    }
}


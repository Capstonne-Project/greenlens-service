using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.UploadUserAvatar;

/// <summary>
/// Upload a new avatar for the authenticated user. Token-based — no userId param.
/// </summary>
public sealed record UploadUserAvatarCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<UploadUserAvatarResponse>>;

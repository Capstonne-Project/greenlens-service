using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Users;
using Greenlens.Application.Features.Users.AcceptDataConsent;
using Greenlens.Application.Features.Users.ExportMyData;
using Greenlens.Application.Features.Users.GetProfile;
using Greenlens.Application.Features.Users.UpdateUserProfile;
using Greenlens.Application.Features.Users.UploadUserAvatar;
using Greenlens.Application.Features.Users.VerifyPhoneFirebase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize]
[Produces("application/json")]
[Tags("👤 Users — User Profile")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("profile")]
    [SwaggerOperation(
        Summary = "Get My Profile",
        Description = "Get the authenticated user's own profile. Uses JWT token — no userId needed.")]
    [SwaggerResponse(200, "User profile", typeof(ApiResponse<UserProfileDto>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    public async Task<IActionResult> GetProfileAsync(CancellationToken ct)
        => (await sender.Send(new GetProfileQuery(), ct)).ToHttp();

    [HttpPut("profile")]
    [SwaggerOperation(
        Summary = "Update My Profile",
        Description = "Update the authenticated user's own profile (name only). Phone changes via Firebase Phone Auth.")]
    [SwaggerResponse(200, "Profile updated", typeof(ApiResponse<UpdateUserProfileResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateProfileAsync(
        [FromBody] UpdateUserProfileCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("avatar")]
    [SwaggerOperation(
        Summary = "Upload Avatar",
        Description = "Upload a new avatar image (jpg/png/webp, max 5MB). Uses JWT token. Stores on R2 Cloudflare.")]
    [SwaggerResponse(200, "Avatar uploaded", typeof(ApiResponse<UploadUserAvatarResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid file type or too large", typeof(ApiResponse))]
    [SwaggerResponse(500, "Storage upload failed", typeof(ApiResponse))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatarAsync(
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

        var command = new UploadUserAvatarCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        return (await sender.Send(command, ct)).ToHttp();
    }

    // ── Phone Verification (Firebase Phone Auth) ──────

    [HttpPost("phone/verify-firebase")]
    [SwaggerOperation(
        Summary = "Verify Phone via Firebase",
        Description = "Verify phone number using a Firebase Phone Auth ID token. " +
                      "FE handles OTP via Firebase SDK, then sends the ID token here.")]
    [SwaggerResponse(200, "Phone verified", typeof(ApiResponse<VerifyPhoneFirebaseResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(409, "Phone already used by another account", typeof(ApiResponse))]
    [SwaggerResponse(422, "Firebase token invalid or missing phone", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyPhoneFirebaseAsync(
        [FromBody] VerifyPhoneFirebaseCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  DATA CONSENT (BR-DAT-005)
    // ═══════════════════════════════════════════

    /// <summary>User explicitly accepts data processing consent (photos, GPS).</summary>
    [HttpPost("me/consent")]
    [Tags("🔒 Data Privacy")]
    [SwaggerOperation(
        Summary = "Accept Data Consent",
        Description = "Hiển thị khi mở app lần đầu. User bấm Đồng ý → gọi API này. " +
            "Phải accept trước khi gửi báo cáo. Idempotent — gọi lại không lỗi.")]
    [SwaggerResponse(200, "Consent accepted")]
    public async Task<IActionResult> AcceptDataConsentAsync(CancellationToken ct)
        => (await sender.Send(new AcceptDataConsentCommand(), ct)).ToHttpNoContent("Đã chấp nhận chính sách xử lý dữ liệu.");

    // ═══════════════════════════════════════════
    // ██  EXPORT PERSONAL DATA (BR-DAT-003)
    // ═══════════════════════════════════════════

    /// <summary>Download all personal data (GDPR / NĐ-13/2023).</summary>
    [HttpGet("me/data-export")]
    [Tags("🔒 Data Privacy")]
    [SwaggerOperation(
        Summary = "Export My Personal Data",
        Description = "Tải xuống toàn bộ dữ liệu cá nhân: profile, reports, notifications, " +
            "gamification. Hỗ trợ JSON (mặc định) và CSV.")]
    [SwaggerResponse(200, "File download")]
    public async Task<IActionResult> ExportMyDataAsync(
        [FromQuery] ExportMyDataFormat format,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new ExportMyDataQuery(format), ct);

        if (!result.IsSuccess)
            return result.ToHttp();

        var data = result.Value!;
        return File(data.Content, data.ContentType, data.FileName);
    }
}


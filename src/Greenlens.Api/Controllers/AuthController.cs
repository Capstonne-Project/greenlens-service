using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Auth.ChangePassword;
using Greenlens.Application.Features.Auth.ForgotPassword;
using Greenlens.Application.Features.Auth.GoogleLogin;
using Greenlens.Application.Features.Auth.Login;
using Greenlens.Application.Features.Auth.RefreshToken;
using Greenlens.Application.Features.Auth.Register;
using Greenlens.Application.Features.Auth.RequestOtp;
using Greenlens.Application.Features.Auth.ResetPassword;
using Greenlens.Application.Features.Auth.VerifyOtp;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Register",
        Description = "Register a new citizen account with email and password. An OTP will be sent for email verification.")]
    [SwaggerResponse(200, "Registration successful", typeof(ApiResponse<RegisterResponse>))]
    [SwaggerResponse(409, "Email already taken", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Login",
        Description = "Login with email and password. Returns accessToken and refreshToken in response data.")]
    [SwaggerResponse(200, "Login successful", typeof(ApiResponse<LoginResponse>))]
    [SwaggerResponse(422, "Invalid credentials", typeof(ApiResponse))]
    [SwaggerResponse(422, "Email not verified", typeof(ApiResponse))]
    [SwaggerResponse(422, "Account locked due to too many failed attempts", typeof(ApiResponse))]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("request-otp")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Request OTP",
        Description = "Send a 6-digit OTP code to the specified email. Valid for 10 minutes. Supports EmailVerification and PasswordReset purposes.")]
    [SwaggerResponse(200, "OTP sent successfully", typeof(ApiResponse<RequestOtpResponse>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    public async Task<IActionResult> RequestOtpAsync(
        [FromBody] RequestOtpCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Verify OTP",
        Description = "Verify the 6-digit OTP code sent to email. For EmailVerification purpose, marks the user's email as verified.")]
    [SwaggerResponse(200, "OTP verified successfully", typeof(ApiResponse<VerifyOtpResponse>))]
    [SwaggerResponse(422, "OTP invalid or expired", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyOtpAsync(
        [FromBody] VerifyOtpCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Forgot Password",
        Description = "Send a password reset OTP to the specified email. Always returns success to prevent email enumeration.")]
    [SwaggerResponse(200, "If email exists, OTP will be sent", typeof(ApiResponse<ForgotPasswordResponse>))]
    public async Task<IActionResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Reset Password",
        Description = "Reset password using OTP code received via email. Revokes all existing refresh tokens.")]
    [SwaggerResponse(200, "Password reset successful", typeof(ApiResponse<ResetPasswordResponse>))]
    [SwaggerResponse(422, "OTP invalid or expired", typeof(ApiResponse))]
    [SwaggerResponse(422, "OTP max attempts exceeded", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromBody] ResetPasswordCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("change-password")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Change Password",
        Description = "Change password for the authenticated user. Requires current password verification.")]
    [SwaggerResponse(200, "Password changed successfully", typeof(ApiResponse<ChangePasswordResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Current password incorrect", typeof(ApiResponse))]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Refresh Token",
        Description = "Exchange a valid refresh token for a new access token and refresh token pair (rotation).")]
    [SwaggerResponse(200, "Token refreshed successfully", typeof(ApiResponse<LoginResponse>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid or expired refresh token", typeof(ApiResponse))]
    public async Task<IActionResult> RefreshTokenAsync(
        [FromBody] RefreshTokenCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();

    [HttpPost("google-login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Login with Google",
        Description = "Login or register using a Firebase Google ID token. Auto-creates account if not exists.")]
    [SwaggerResponse(200, "Google login successful", typeof(ApiResponse<LoginResponse>))]
    [SwaggerResponse(422, "Google authentication failed", typeof(ApiResponse))]
    public async Task<IActionResult> GoogleLoginAsync(
        [FromBody] GoogleLoginCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp();
}

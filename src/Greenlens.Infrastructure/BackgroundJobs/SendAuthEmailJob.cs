using Greenlens.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// Sends auth OTP / password-reset emails out of band (Hangfire).
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-013, BR-SYS-001.
/// HTTP success is decoupled from SMTP delivery — failures retry here; user may resend OTP.
/// </remarks>
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 600])]
internal sealed class SendAuthEmailJob(
    IEmailSender emailSender,
    ILogger<SendAuthEmailJob> logger)
{
    public async Task SendOtpAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
    {
        try
        {
            await emailSender.SendOtpAsync(toEmail, otpCode, purpose, ct).ConfigureAwait(false);
            logger.LogInformation("Auth OTP email job completed for purpose {Purpose}", purpose);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auth OTP email job failed for purpose {Purpose}", purpose);
            throw;
        }
    }

    public async Task SendPasswordResetAsync(string toEmail, string otpCode, CancellationToken ct = default)
    {
        try
        {
            await emailSender.SendPasswordResetAsync(toEmail, otpCode, ct).ConfigureAwait(false);
            logger.LogInformation("Password reset email job completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Password reset email job failed");
            throw;
        }
    }
}

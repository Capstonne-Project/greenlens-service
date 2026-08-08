using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Email;

/// <summary>
/// No-op email sender for local dev when Smtp:Enabled=false — avoids blocking on SMTP timeouts.
/// </summary>
internal sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendOtpAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
    {
        logger.LogInformation("NoOpEmailSender: skipped OTP email (purpose {Purpose})", purpose);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string otpCode, CancellationToken ct = default)
    {
        logger.LogInformation("NoOpEmailSender: skipped password-reset email");
        return Task.CompletedTask;
    }

    public Task SendNotificationEmailAsync(string toEmail, string subject, string message, CancellationToken ct = default)
    {
        logger.LogInformation("NoOpEmailSender: skipped notification email with subject {Subject}", subject);
        return Task.CompletedTask;
    }
}

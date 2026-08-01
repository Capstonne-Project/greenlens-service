using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Services;

/// <summary>
/// Enqueues auth-related emails on Hangfire after OTP is persisted.
/// </summary>
/// <remarks>Implements: BR-AUTH-013, BR-SYS-001.</remarks>
internal sealed class AuthEmailScheduler(
    IBackgroundJobClient jobs,
    ILogger<AuthEmailScheduler> logger) : IAuthEmailScheduler
{
    public bool TryEnqueueOtpEmail(string toEmail, string otpCode, string purpose)
    {
        try
        {
            jobs.Enqueue<SendAuthEmailJob>(
                j => j.SendOtpAsync(toEmail, otpCode, purpose, CancellationToken.None));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue OTP email job");
            return false;
        }
    }

    public bool TryEnqueuePasswordResetEmail(string toEmail, string otpCode)
    {
        try
        {
            jobs.Enqueue<SendAuthEmailJob>(
                j => j.SendPasswordResetAsync(toEmail, otpCode, CancellationToken.None));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue password-reset email job");
            return false;
        }
    }
}

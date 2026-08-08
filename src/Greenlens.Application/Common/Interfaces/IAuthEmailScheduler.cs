namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Schedules auth OTP / password-reset emails via Hangfire so auth endpoints stay fast (BR-SYS-001).
/// </summary>
public interface IAuthEmailScheduler
{
    /// <summary>Enqueue OTP email delivery. Returns false if Hangfire enqueue failed.</summary>
    bool TryEnqueueOtpEmail(string toEmail, string otpCode, string purpose);

    /// <summary>Enqueue password-reset OTP email. Returns false if Hangfire enqueue failed.</summary>
    bool TryEnqueuePasswordResetEmail(string toEmail, string otpCode);
}

namespace Greenlens.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendOtpAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string otpCode, CancellationToken ct = default);
}

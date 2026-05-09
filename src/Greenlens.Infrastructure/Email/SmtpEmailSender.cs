using System.Net;
using System.Net.Mail;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _smtp = options.Value;

    public async Task SendOtpAsync(string toEmail, string otpCode, string purpose, CancellationToken ct = default)
    {
        var subject = purpose == "EmailVerification"
            ? "GreenLens - Xác thực email"
            : "GreenLens - Mã OTP";

        var body = $"""
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <div style="background: linear-gradient(135deg, #10b981, #059669); padding: 20px; border-radius: 8px 8px 0 0;">
                    <h1 style="color: white; margin: 0;">🌿 GreenLens</h1>
                </div>
                <div style="padding: 30px; background: #f9fafb; border-radius: 0 0 8px 8px;">
                    <p>Xin chào,</p>
                    <p>Mã OTP của bạn là:</p>
                    <div style="background: white; border: 2px dashed #10b981; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;">
                        <span style="font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #059669;">{otpCode}</span>
                    </div>
                    <p style="color: #6b7280;">Mã này có hiệu lực trong <strong>10 phút</strong>.</p>
                    <p style="color: #6b7280;">Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>
                </div>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, subject, body, ct).ConfigureAwait(false);
    }

    public async Task SendPasswordResetAsync(string toEmail, string otpCode, CancellationToken ct = default)
    {
        var body = $"""
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <div style="background: linear-gradient(135deg, #10b981, #059669); padding: 20px; border-radius: 8px 8px 0 0;">
                    <h1 style="color: white; margin: 0;">🌿 GreenLens</h1>
                </div>
                <div style="padding: 30px; background: #f9fafb; border-radius: 0 0 8px 8px;">
                    <p>Xin chào,</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu. Mã OTP của bạn là:</p>
                    <div style="background: white; border: 2px dashed #ef4444; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;">
                        <span style="font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #dc2626;">{otpCode}</span>
                    </div>
                    <p style="color: #6b7280;">Mã này có hiệu lực trong <strong>10 phút</strong>.</p>
                    <p style="color: #ef4444;"><strong>⚠️ Nếu bạn không yêu cầu, vui lòng đổi mật khẩu ngay.</strong></p>
                </div>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, "GreenLens - Đặt lại mật khẩu", body, ct).ConfigureAwait(false);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new NetworkCredential(_smtp.Username, _smtp.Password),
                EnableSsl = _smtp.EnableSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, ct).ConfigureAwait(false);
            logger.LogInformation("Email sent to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FromEmail { get; init; } = default!;
    public string FromName { get; init; } = "GreenLens";
    public bool EnableSsl { get; init; } = true;
}

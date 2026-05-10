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
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;">
                <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color: #f3f4f6; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);">
                                <tr>
                                    <td style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px 40px; text-align: center;">
                                        <h1 style="color: #ffffff; margin: 0; font-size: 28px; font-weight: 700; letter-spacing: 1px;">🌿 GreenLens</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 40px;">
                                        <p style="margin: 0 0 20px 0; color: #374151; font-size: 16px; line-height: 1.6;">Xin chào,</p>
                                        <p style="margin: 0 0 30px 0; color: #374151; font-size: 16px; line-height: 1.6;">Mã OTP của bạn tại GreenLens là:</p>
                                        
                                        <table width="100%" cellpadding="0" cellspacing="0" border="0">
                                            <tr>
                                                <td align="center" style="padding: 20px; background-color: #f0fdf4; border: 2px dashed #34d399; border-radius: 8px;">
                                                    <span style="font-family: monospace; font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #059669;">{otpCode}</span>
                                                </td>
                                            </tr>
                                        </table>
                                        
                                        <p style="margin: 30px 0 10px 0; color: #6b7280; font-size: 14px; text-align: center;">Mã này có hiệu lực trong <strong>10 phút</strong>.</p>
                                        <p style="margin: 0; color: #9ca3af; font-size: 13px; text-align: center;">Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="background-color: #f9fafb; padding: 20px 40px; text-align: center; border-top: 1px solid #e5e7eb;">
                                        <p style="margin: 0; color: #9ca3af; font-size: 12px;">&copy; {DateTime.UtcNow.Year} GreenLens. Cảm ơn bạn đã đồng hành cùng sứ mệnh bảo vệ môi trường xanh.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, subject, body, ct).ConfigureAwait(false);
    }

    public async Task SendPasswordResetAsync(string toEmail, string otpCode, CancellationToken ct = default)
    {
        var body = $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;">
                <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color: #f3f4f6; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width: 600px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);">
                                <tr>
                                    <td style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px 40px; text-align: center;">
                                        <h1 style="color: #ffffff; margin: 0; font-size: 28px; font-weight: 700; letter-spacing: 1px;">🌿 GreenLens</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 40px;">
                                        <p style="margin: 0 0 20px 0; color: #374151; font-size: 16px; line-height: 1.6;">Xin chào,</p>
                                        <p style="margin: 0 0 30px 0; color: #374151; font-size: 16px; line-height: 1.6;">Bạn đã yêu cầu đặt lại mật khẩu. Sử dụng mã OTP sau để tiếp tục:</p>
                                        
                                        <table width="100%" cellpadding="0" cellspacing="0" border="0">
                                            <tr>
                                                <td align="center" style="padding: 20px; background-color: #fef2f2; border: 2px dashed #f87171; border-radius: 8px;">
                                                    <span style="font-family: monospace; font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #dc2626;">{otpCode}</span>
                                                </td>
                                            </tr>
                                        </table>
                                        
                                        <p style="margin: 30px 0 10px 0; color: #6b7280; font-size: 14px; text-align: center;">Mã này có hiệu lực trong <strong>10 phút</strong>.</p>
                                        <p style="margin: 0; color: #ef4444; font-size: 14px; text-align: center; font-weight: 600;">⚠️ Nếu bạn không yêu cầu, vui lòng đổi mật khẩu ngay lập tức.</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="background-color: #f9fafb; padding: 20px 40px; text-align: center; border-top: 1px solid #e5e7eb;">
                                        <p style="margin: 0; color: #9ca3af; font-size: 12px;">&copy; {DateTime.UtcNow.Year} GreenLens. Cảm ơn bạn đã đồng hành cùng sứ mệnh bảo vệ môi trường xanh.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
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

using System.Net;
using System.Net.Mail;
using System.Text;
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

    /// <summary>BR-NTF-001: Send a notification email with standard GreenLens template.</summary>
    public async Task SendNotificationEmailAsync(string toEmail, string subject, string message, CancellationToken ct = default)
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
                                        <h2 style="margin: 0 0 20px 0; color: #1f2937; font-size: 20px; font-weight: 600;">{WebUtility.HtmlEncode(subject)}</h2>
                                        {FormatNotificationMessageHtml(message)}
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

        await SendEmailAsync(toEmail, $"GreenLens - {subject}", body, ct).ConfigureAwait(false);
    }

    /// <summary>Renders plain-text notification body as HTML with line breaks and credential highlight boxes.</summary>
    internal static string FormatNotificationMessageHtml(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var lines = message.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var html = new StringBuilder();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (TryParseInlineCredential(line, out var inlineLabel, out var inlineValue))
            {
                AppendCredentialBlock(html, inlineLabel, inlineValue);
                continue;
            }

            if (IsCredentialLabel(line, out var label))
            {
                string? value = null;
                if (i + 1 < lines.Length && !string.IsNullOrWhiteSpace(lines[i + 1]))
                {
                    value = lines[i + 1].Trim();
                    i++;
                }

                AppendCredentialBlock(html, label, value ?? string.Empty);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                html.Append("<div style=\"height:8px;\"></div>");
                continue;
            }

            html.Append(
                $"""<p style="margin:0 0 12px 0;color:#374151;font-size:16px;line-height:1.6;">{WebUtility.HtmlEncode(line)}</p>""");
        }

        return html.ToString();
    }

    private static bool IsCredentialLabel(string line, out string label)
    {
        var trimmed = line.Trim();
        if (trimmed is "Email đăng nhập:" or "Login email:")
        {
            label = trimmed;
            return true;
        }

        if (trimmed is "Mật khẩu tạm:" or "Temporary password:")
        {
            label = trimmed;
            return true;
        }

        label = string.Empty;
        return false;
    }

    private static bool TryParseInlineCredential(string line, out string label, out string value)
    {
        (string Prefix, string Label)[] patterns =
        [
            ("Email đăng nhập: ", "Email đăng nhập:"),
            ("Login email: ", "Login email:"),
            ("Mật khẩu tạm: ", "Mật khẩu tạm:"),
            ("Temporary password: ", "Temporary password:"),
        ];

        foreach (var (prefix, credentialLabel) in patterns)
        {
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            label = credentialLabel;
            value = line[prefix.Length..].Trim();
            return true;
        }

        label = string.Empty;
        value = string.Empty;
        return false;
    }

    private static void AppendCredentialBlock(StringBuilder html, string label, string value)
    {
        html.Append(
            $"""<p style="margin:16px 0 6px 0;color:#374151;font-size:14px;font-weight:600;">{WebUtility.HtmlEncode(label)}</p>""");

        html.Append(
            $"""
            <table width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:0 0 4px 0;">
                <tr>
                    <td style="padding:12px 16px;background-color:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;">
                        <span style="font-family:Consolas,'Courier New',monospace;font-size:15px;color:#065f46;word-break:break-all;user-select:all;">{WebUtility.HtmlEncode(value)}</span>
                    </td>
                </tr>
            </table>
            """);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            logger.LogInformation(
                "Sending email via SMTP host {SmtpHost} from {FromEmail}",
                _smtp.Host,
                _smtp.FromEmail);

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
    public bool Enabled { get; init; } = true;
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FromEmail { get; init; } = default!;
    public string FromName { get; init; } = "GreenLens";
    public bool EnableSsl { get; init; } = true;
}

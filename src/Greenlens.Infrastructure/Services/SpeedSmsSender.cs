using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Services;

/// <summary>
/// SpeedSMS API adapter for sending OTP SMS messages.
/// API docs: https://speedsms.vn/sms-api-service/
/// </summary>
public sealed class SpeedSmsSender(
    HttpClient httpClient,
    IOptions<SpeedSmsOptions> options,
    ILogger<SpeedSmsSender> logger) : ISmsSender
{
    private readonly SpeedSmsOptions _options = options.Value;

    public async Task<bool> SendOtpAsync(string phoneNumber, string otpCode, CancellationToken ct = default)
    {
        try
        {
            var content = $"GreenLens: Ma xac thuc cua ban la {otpCode}. Het han sau 5 phut.";

            // SpeedSMS uses Basic Auth with access token as username
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccessToken}:"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var requestBody = new
            {
                to = new[] { phoneNumber },
                content,
                sms_type = 4, // type 4 = brandname mặc định (Verify/Notify)
                sender = "Verify"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync(
                $"{_options.BaseUrl}/sms/send",
                jsonContent,
                ct).ConfigureAwait(false);

            var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("SpeedSMS API error. Status: {Status}, Body: {Body}",
                    response.StatusCode, responseBody);
                return false;
            }

            // Parse response to check SpeedSMS-specific status
            using var doc = JsonDocument.Parse(responseBody);
            var status = doc.RootElement.GetProperty("status").GetString();

            if (status != "success")
            {
                var errorCode = doc.RootElement.GetProperty("code").GetString();
                var errorMessage = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown";
                logger.LogError("SpeedSMS send failed. Code: {Code}, Message: {Message}", errorCode, errorMessage);
                return false;
            }

            logger.LogInformation("SMS OTP sent to {Phone} via SpeedSMS", phoneNumber[..^4] + "****");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS OTP to {Phone}", phoneNumber[..^4] + "****");
            return false;
        }
    }
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Identity;

/// <summary>Cloudflare Turnstile CAPTCHA verifier. BR-AUTH-014.</summary>
public sealed class TurnstileCaptchaValidator(
    IHttpClientFactory httpClientFactory,
    IOptions<CaptchaOptions> options,
    ILogger<TurnstileCaptchaValidator> logger) : ICaptchaValidator
{
    public async Task<bool> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (!options.Value.Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
        {
            logger.LogWarning("Captcha enabled but SecretKey is missing");
            return false;
        }

        var client = httpClientFactory.CreateClient("Turnstile");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = options.Value.SecretKey,
            ["response"] = token,
        });

        var response = await client.PostAsync(string.Empty, content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Turnstile verify HTTP {StatusCode}", response.StatusCode);
            return false;
        }

        var body = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(ct)
            .ConfigureAwait(false);

        return body?.Success == true;
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }
    }
}

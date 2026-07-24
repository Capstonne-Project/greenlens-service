namespace Greenlens.Application.Common.Interfaces;

/// <summary>Validates CAPTCHA tokens (Cloudflare Turnstile). BR-AUTH-014.</summary>
public interface ICaptchaValidator
{
    Task<bool> ValidateAsync(string token, CancellationToken ct = default);
}

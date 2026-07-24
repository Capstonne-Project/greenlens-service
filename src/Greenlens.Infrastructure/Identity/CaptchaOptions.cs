namespace Greenlens.Infrastructure.Identity;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    public bool Enabled { get; init; }

    public string SecretKey { get; init; } = string.Empty;
}

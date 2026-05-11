namespace Greenlens.Infrastructure.Options;

/// <summary>Configuration for SpeedSMS API integration.</summary>
public sealed class SpeedSmsOptions
{
    public const string Section = "SpeedSms";

    /// <summary>API Access Token from https://connect.speedsms.vn</summary>
    public string AccessToken { get; init; } = default!;

    /// <summary>SpeedSMS API base URL.</summary>
    public string BaseUrl { get; init; } = "https://api.speedsms.vn/index.php";
}

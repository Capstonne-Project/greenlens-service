namespace Greenlens.Infrastructure.Configuration;

/// <summary>
/// Tên kênh Redis pub/sub dùng chung giữa các API instance.
/// </summary>
internal static class SystemSettingsCacheChannels
{
    /// <summary>
    /// Admin PATCH/Reset publish lên kênh này → mọi instance subscribe và reload settings từ DB.
    /// </summary>
    public const string Invalidate = "greenlens:cache:system-settings:invalidate";
}

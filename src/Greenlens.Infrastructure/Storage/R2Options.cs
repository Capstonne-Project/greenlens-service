namespace Greenlens.Infrastructure.Storage;

public sealed class R2Options
{
    public string AccountId { get; init; } = default!;
    public string AccessKeyId { get; init; } = default!;
    public string SecretAccessKey { get; init; } = default!;
    public string BucketName { get; init; } = default!;

    /// <summary>
    /// Public base URL for accessing uploaded files (e.g. https://pub-xxx.r2.dev).
    /// </summary>
    public string PublicUrl { get; init; } = default!;
}

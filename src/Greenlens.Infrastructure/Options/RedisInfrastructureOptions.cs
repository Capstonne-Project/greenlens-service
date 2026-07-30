namespace Greenlens.Infrastructure.Options;

/// <summary>
/// Redis infrastructure options — P0: required in staging/production for multi-instance rate limits.
/// </summary>
public sealed class RedisInfrastructureOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// When true, startup fails if <c>ConnectionStrings:Redis</c> is missing (staging/production).
    /// </summary>
    public bool Required { get; init; }
}

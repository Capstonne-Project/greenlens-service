using System.ComponentModel.DataAnnotations;

namespace Greenlens.Infrastructure.Ai;

public sealed class AiOptions
{
    [Required]
    public string BaseUrl { get; init; } = default!;

    /// <summary>Timeout in seconds for AI classification call. BR-AI-006: default 5s.</summary>
    public int TimeoutSeconds { get; init; } = 5;
}

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Per-request idempotency state (set by API filter when replaying a cached response).
/// </summary>
public interface IIdempotencyContext
{
    bool IsReplay { get; }
}

namespace Greenlens.Application.Common.Idempotency;

/// <summary>HttpContext.Items keys shared between API filter and infrastructure context.</summary>
public static class IdempotencyHttpItems
{
    public const string IsReplayKey = "Idempotency.IsReplay";
    public const string ScopeKey = "Idempotency.ScopeKey";
}

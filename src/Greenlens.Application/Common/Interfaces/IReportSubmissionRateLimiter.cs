namespace Greenlens.Application.Common.Interfaces;

/// <summary>BR-REP-010: sliding-window submit quota per Citizen (5/h, 20/24h, 1h lock on breach).</summary>
public interface IReportSubmissionRateLimiter
{
    /// <summary>
    /// Atomically checks quota and records one submission when allowed.
    /// When denied, <see cref="ReportSubmissionRateLimitResult.RetryAfterMinutes"/> is populated.
    /// </summary>
    Task<ReportSubmissionRateLimitResult> TryAcquireAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record ReportSubmissionRateLimitResult(bool IsAllowed, int RetryAfterMinutes);

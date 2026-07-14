using System.Collections.Concurrent;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.RateLimiting;

/// <summary>
/// BR-REP-010 fallback when Redis is unavailable (local dev / tests).
/// Not suitable for multi-instance production — use <see cref="RedisReportSubmissionRateLimiter"/>.
/// </summary>
public sealed class InMemoryReportSubmissionRateLimiter : IReportSubmissionRateLimiter
{
    private const int MaxPerHour = 5;
    private const int MaxPerDay = 20;
    private static readonly TimeSpan HourWindow = TimeSpan.FromHours(1);
    private static readonly TimeSpan DayWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan LockDuration = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<Guid, UserRateState> _states = new();

    public Task<ReportSubmissionRateLimitResult> TryAcquireAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var state = _states.GetOrAdd(userId, _ => new UserRateState());

        lock (state.Sync)
        {
            if (state.LockedUntilUtc is { } lockedUntil && lockedUntil > now)
            {
                var retry = (int)Math.Ceiling((lockedUntil - now).TotalMinutes);
                return Task.FromResult(new ReportSubmissionRateLimitResult(false, Math.Max(retry, 1)));
            }

            state.LockedUntilUtc = null;
            state.Submissions.RemoveAll(t => now - t > DayWindow);

            var hourCount = state.Submissions.Count(t => now - t <= HourWindow);
            if (hourCount >= MaxPerHour || state.Submissions.Count >= MaxPerDay)
            {
                state.LockedUntilUtc = now.Add(LockDuration);
                return Task.FromResult(new ReportSubmissionRateLimitResult(false, 60));
            }

            state.Submissions.Add(now);
            return Task.FromResult(new ReportSubmissionRateLimitResult(true, 0));
        }
    }

    private sealed class UserRateState
    {
        public object Sync { get; } = new();
        public List<DateTime> Submissions { get; } = [];
        public DateTime? LockedUntilUtc { get; set; }
    }
}

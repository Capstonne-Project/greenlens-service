using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Greenlens.Infrastructure.RateLimiting;

/// <summary>BR-REP-010: Redis sorted-set sliding window with configurable limits.</summary>
public sealed class RedisReportSubmissionRateLimiter(
    IConnectionMultiplexer redis,
    ISystemSettingsProvider systemSettings) : IReportSubmissionRateLimiter
{
    private const long HourWindowMs = 3_600_000;
    private const long DayWindowMs = 86_400_000;

    private static readonly LuaScript AcquireScript = LuaScript.Prepare(@"
local lockKey = @lockKey
local hourKey = @hourKey
local dayKey = @dayKey
local now = tonumber(@now)
local member = @member
local hourWindow = tonumber(@hourWindow)
local dayWindow = tonumber(@dayWindow)
local maxHour = tonumber(@maxHour)
local maxDay = tonumber(@maxDay)
local lockSeconds = tonumber(@lockSeconds)

local lockTtl = redis.call('TTL', lockKey)
if lockTtl > 0 then
  return {0, lockTtl}
end

redis.call('ZREMRANGEBYSCORE', hourKey, 0, now - hourWindow)
local hourCount = redis.call('ZCARD', hourKey)
if hourCount >= maxHour then
  redis.call('SET', lockKey, '1', 'EX', lockSeconds)
  return {0, lockSeconds}
end

redis.call('ZREMRANGEBYSCORE', dayKey, 0, now - dayWindow)
local dayCount = redis.call('ZCARD', dayKey)
if dayCount >= maxDay then
  redis.call('SET', lockKey, '1', 'EX', lockSeconds)
  return {0, lockSeconds}
end

redis.call('ZADD', hourKey, now, member)
redis.call('ZADD', dayKey, now, member)
redis.call('EXPIRE', hourKey, 3600)
redis.call('EXPIRE', dayKey, 90000)
return {1, 0}
");

    public async Task<ReportSubmissionRateLimitResult> TryAcquireAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (maxPerHour, maxPerDay, lockSeconds) = ModuleSystemSettings.SubmitRateLimits(systemSettings);
        var db = redis.GetDatabase();
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var member = Guid.NewGuid().ToString("N");

        var result = await db.ScriptEvaluateAsync(
            AcquireScript,
            new
            {
                lockKey = (RedisKey)$"ratelimit:report:lock:{userId:N}",
                hourKey = (RedisKey)$"ratelimit:report:hour:{userId:N}",
                dayKey = (RedisKey)$"ratelimit:report:day:{userId:N}",
                now = nowMs,
                member,
                hourWindow = HourWindowMs,
                dayWindow = DayWindowMs,
                maxHour = maxPerHour,
                maxDay = maxPerDay,
                lockSeconds
            }).ConfigureAwait(false);

        if (result.IsNull)
            return new ReportSubmissionRateLimitResult(false, 60);

        var allowed = (int)result[0]! == 1;
        var retrySeconds = (int)result[1]!;
        var retryMinutes = allowed ? 0 : Math.Max(1, (int)Math.Ceiling(retrySeconds / 60.0));
        return new ReportSubmissionRateLimitResult(allowed, retryMinutes);
    }
}

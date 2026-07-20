using Greenlens.Infrastructure.RateLimiting;

namespace Greenlens.Application.UnitTests;

public sealed class ReportSubmissionRateLimiterTests
{
    private readonly InMemoryReportSubmissionRateLimiter _sut = new();

    [Fact]
    public async Task TryAcquire_FirstFiveWithinHour_Allowed_BR_REP_010()
    {
        var userId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            var result = await _sut.TryAcquireAsync(userId, CancellationToken.None);
            Assert.True(result.IsAllowed, $"submission {i + 1} should be allowed");
        }
    }

    [Fact]
    public async Task TryAcquire_SixthWithinHour_DeniedWithRetry_BR_REP_010()
    {
        var userId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
            await _sut.TryAcquireAsync(userId, CancellationToken.None);

        var denied = await _sut.TryAcquireAsync(userId, CancellationToken.None);

        Assert.False(denied.IsAllowed);
        Assert.True(denied.RetryAfterMinutes > 0);
    }
}

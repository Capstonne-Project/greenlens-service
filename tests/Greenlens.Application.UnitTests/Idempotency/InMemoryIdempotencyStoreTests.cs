using FluentAssertions;
using Greenlens.Application.Common.Idempotency;
using Greenlens.Infrastructure.Idempotency;

namespace Greenlens.Application.UnitTests.Idempotency;

public sealed class InMemoryIdempotencyStoreTests
{
    private readonly InMemoryIdempotencyStore _sut = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private const string ScopeKey = "user:abc:post:/v1/reports:key-1";
    private const string BodyHash = "BODY_HASH_A";

    [Fact]
    public async Task TryAcquire_FirstRequest_Acquired()
    {
        var result = await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);

        result.Outcome.Should().Be(IdempotencyAcquireOutcome.Acquired);
    }

    [Fact]
    public async Task TryAcquire_CompletedSameBody_ReplaysCachedResponse()
    {
        const string cachedJson = """{"code":"SUCCESS","status":201,"data":{"reportId":"x"}}""";

        await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);
        await _sut.CompleteAsync(ScopeKey, 201, cachedJson, CancellationToken.None);

        var replay = await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);

        replay.Outcome.Should().Be(IdempotencyAcquireOutcome.Replay);
        replay.Cached!.StatusCode.Should().Be(201);
        replay.Cached.BodyJson.Should().Be(cachedJson);
    }

    [Fact]
    public async Task TryAcquire_CompletedDifferentBody_ReturnsBodyMismatch()
    {
        await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);
        await _sut.CompleteAsync(ScopeKey, 201, "{}", CancellationToken.None);

        var replay = await _sut.TryAcquireAsync(ScopeKey, "OTHER_BODY_HASH", Ttl, CancellationToken.None);

        replay.Outcome.Should().Be(IdempotencyAcquireOutcome.BodyMismatch);
    }

    [Fact]
    public async Task TryAcquire_WhileProcessing_ReturnsInProgress()
    {
        await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);

        var second = await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);

        second.Outcome.Should().Be(IdempotencyAcquireOutcome.InProgress);
    }

    [Fact]
    public async Task Release_AfterFailure_AllowsRetryWithSameKey()
    {
        await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);
        await _sut.ReleaseAsync(ScopeKey, CancellationToken.None);

        var retry = await _sut.TryAcquireAsync(ScopeKey, BodyHash, Ttl, CancellationToken.None);

        retry.Outcome.Should().Be(IdempotencyAcquireOutcome.Acquired);
    }
}

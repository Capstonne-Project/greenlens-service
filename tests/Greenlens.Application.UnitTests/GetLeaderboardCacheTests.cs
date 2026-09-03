using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification;
using Greenlens.Application.Features.Gamification.GetLeaderboard;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class GetLeaderboardCacheTests
{
    private readonly IUserPointsRepository _userPointsRepo = Substitute.For<IUserPointsRepository>();
    private readonly ILeaderboardCache _leaderboardCache = Substitute.For<ILeaderboardCache>();

    [Fact]
    public void LeaderboardCacheKeys_AllTime_IncludesTop_BR_GAM_005()
    {
        var key = LeaderboardCacheKeys.Build(LeaderboardPeriod.AllTime, top: 10);

        key.Should().Be("gamification:leaderboard:all-time:top:10");
    }

    [Fact]
    public void LeaderboardCacheKeys_Monthly_IncludesYearMonth_BR_GAM_005()
    {
        var key = LeaderboardCacheKeys.Build(
            LeaderboardPeriod.Monthly,
            top: 20,
            year: 2026,
            month: 9);

        key.Should().Be("gamification:leaderboard:monthly:2026:9:top:20");
    }

    [Fact]
    public async Task Handle_CacheHit_SkipsRepositoryQuery_BR_GAM_005()
    {
        var cached = new LeaderboardResponse(
            LeaderboardPeriod.AllTime,
            null,
            null,
            null,
            null,
            [new LeaderboardEntry(1, Guid.NewGuid(), "Cached User", null, 100, 2)]);

        var cacheKey = LeaderboardCacheKeys.Build(LeaderboardPeriod.AllTime, top: 10);
        _leaderboardCache.GetAsync(cacheKey, Arg.Any<CancellationToken>())
            .Returns(cached);

        var handler = new GetLeaderboardQueryHandler(
            _userPointsRepo,
            _leaderboardCache,
            NullLogger<GetLeaderboardQueryHandler>.Instance);

        var result = await handler.Handle(new GetLeaderboardQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(cached);
        _userPointsRepo.DidNotReceive().QueryAsNoTracking();
        await _leaderboardCache.DidNotReceive()
            .SetAsync(Arg.Any<string>(), Arg.Any<LeaderboardResponse>(), Arg.Any<CancellationToken>());
    }
}

using Greenlens.Domain.Common;

namespace Greenlens.Domain.UnitTests;

public sealed class DuplicateDetectionSourcesTests
{
    [Fact]
    public void IsTier1PendingAi_AcceptsCurrentAndLegacyTier1_BR_REP_030()
    {
        Assert.True(DuplicateDetectionSources.IsTier1PendingAi(DuplicateDetectionSources.Tier1));
        Assert.True(DuplicateDetectionSources.IsTier1PendingAi(DuplicateDetectionSources.Tier1Legacy));
    }

    [Fact]
    public void IsTier1PendingAi_RejectsTier2Ai_BR_REP_030()
    {
        Assert.False(DuplicateDetectionSources.IsTier1PendingAi(DuplicateDetectionSources.Tier2Ai));
        Assert.False(DuplicateDetectionSources.IsTier1PendingAi(DuplicateDetectionSources.Tier2AiLegacy));
    }
}

using Greenlens.Application.Common.Mappings;
using Xunit;

namespace Greenlens.Application.UnitTests;

public sealed class AiPollutionClassMapperTests
{
    [Theory]
    [InlineData("Trash", "TRASH")]
    [InlineData("TRASH", "TRASH")]
    [InlineData("Water", "WASTEWATER")]
    [InlineData("WATER", "WASTEWATER")]
    [InlineData("Smoke", "SMOKE")]
    [InlineData("Chemical", "CHEMICAL")]
    public void ToCategoryCode_KnownAiClass_ReturnsDbCode_BR_AI_001(
        string aiClass,
        string expectedCode)
    {
        Assert.Equal(expectedCode, AiPollutionClassMapper.ToCategoryCode(aiClass));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NOISE")]
    public void ToCategoryCode_UnknownOrEmpty_ReturnsNull(string? aiClass)
    {
        Assert.Null(AiPollutionClassMapper.ToCategoryCode(aiClass));
    }
}

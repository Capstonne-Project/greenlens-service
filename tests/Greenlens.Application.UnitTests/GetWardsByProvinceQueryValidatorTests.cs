using Greenlens.Application.Features.Catalog.GetWardsByProvince;

namespace Greenlens.Application.UnitTests;

public sealed class GetWardsByProvinceQueryValidatorTests
{
    private readonly GetWardsByProvinceQueryValidator _sut = new();

    [Theory]
    [InlineData("79")]
    [InlineData("01")]
    public void Validate_OfficialTwoDigitCode_IsValid_BR_REP_003(string code)
    {
        var result = _sut.Validate(new GetWardsByProvinceQuery(code));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("7")]
    [InlineData("799")]
    [InlineData("AB")]
    [InlineData("7A")]
    public void Validate_InvalidProvinceCode_IsInvalid_BR_REP_003(string code)
    {
        var result = _sut.Validate(new GetWardsByProvinceQuery(code));

        Assert.False(result.IsValid);
    }
}

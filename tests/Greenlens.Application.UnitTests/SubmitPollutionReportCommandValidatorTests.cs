using Greenlens.Application.Features.Reports.SubmitPollutionReport;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class SubmitPollutionReportCommandValidatorTests
{
    private readonly SubmitPollutionReportCommandValidator _sut = new();

    private static SubmitPollutionReportCommand BaseCommand() =>
        new(
            CategoryId: Guid.NewGuid(),
            Severity: Severity.Medium,
            Description: null,
            Latitude: 10.5m,
            Longitude: 106.5m,
            Address: null,
            WardCode: null,
            ProvinceCode: null,
            IsAnonymous: true,
            Images:
            [
                new SubmitPollutionReportImageItem(
                    "https://cdn.example.com/a.jpg",
                    "image/jpeg",
                    100)
            ]);

    [Fact]
    public void Validate_ProvinceWithoutWard_IsInvalid_BR_REP_003()
    {
        var cmd = BaseCommand() with { ProvinceCode = "79", WardCode = null };
        var result = _sut.Validate(cmd);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WardWithoutProvince_IsInvalid_BR_REP_003()
    {
        var cmd = BaseCommand() with { ProvinceCode = null, WardCode = "12345" };
        var result = _sut.Validate(cmd);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_BothAdministrativeCodesOmitted_IsValid_BR_REP_003()
    {
        var cmd = BaseCommand();
        var result = _sut.Validate(cmd);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PairedValidCodes_FormatOk_BR_REP_003()
    {
        var cmd = BaseCommand() with { ProvinceCode = "79", WardCode = "12345" };
        var result = _sut.Validate(cmd);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidProvincePattern_IsInvalid_BR_REP_003()
    {
        var cmd = BaseCommand() with { ProvinceCode = "A", WardCode = "12345" };
        var result = _sut.Validate(cmd);

        Assert.False(result.IsValid);
    }
}

using Greenlens.Application.Features.Reports.SubmitPollutionReport;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class SubmitPollutionReportCommandValidatorTests
{
    private readonly SubmitPollutionReportCommandValidator _sut = new();

    private static SubmitPollutionReportCommand ManualBase() =>
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
            TempImageId: null,
            Images:
            [
                new SubmitPollutionReportImageItem(
                    "https://cdn.example.com/a.jpg",
                    "image/jpeg",
                    100)
            ]);

    private static SubmitPollutionReportCommand AiBase() =>
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
            TempImageId: Guid.NewGuid().ToString("N"),
            Images: null);

    // ── Mutually exclusive image source ──────────────────────────────────

    [Fact]
    public void Validate_BothSourcesProvided_IsInvalid()
    {
        var cmd = ManualBase() with { TempImageId = Guid.NewGuid().ToString("N") };
        Assert.False(_sut.Validate(cmd).IsValid);
    }

    [Fact]
    public void Validate_NeitherSourceProvided_IsInvalid()
    {
        var cmd = ManualBase() with { Images = null };
        Assert.False(_sut.Validate(cmd).IsValid);
    }

    [Fact]
    public void Validate_AiFlow_Valid()
    {
        Assert.True(_sut.Validate(AiBase()).IsValid);
    }

    [Fact]
    public void Validate_ManualFlow_Valid()
    {
        Assert.True(_sut.Validate(ManualBase()).IsValid);
    }

    // ── GPS bounds BR-REP-003 ─────────────────────────────────────────────

    [Fact]
    public void Validate_ProvinceWithoutWard_IsInvalid_BR_REP_003()
    {
        var cmd = ManualBase() with { ProvinceCode = "79", WardCode = null };
        Assert.False(_sut.Validate(cmd).IsValid);
    }

    [Fact]
    public void Validate_WardWithoutProvince_IsInvalid_BR_REP_003()
    {
        var cmd = ManualBase() with { ProvinceCode = null, WardCode = "12345" };
        Assert.False(_sut.Validate(cmd).IsValid);
    }

    [Fact]
    public void Validate_BothAdministrativeCodesOmitted_IsValid_BR_REP_003()
    {
        Assert.True(_sut.Validate(ManualBase()).IsValid);
    }

    [Fact]
    public void Validate_PairedValidCodes_FormatOk_BR_REP_003()
    {
        var cmd = ManualBase() with { ProvinceCode = "79", WardCode = "12345" };
        Assert.True(_sut.Validate(cmd).IsValid);
    }

    [Fact]
    public void Validate_InvalidProvincePattern_IsInvalid_BR_REP_003()
    {
        var cmd = ManualBase() with { ProvinceCode = "A", WardCode = "12345" };
        Assert.False(_sut.Validate(cmd).IsValid);
    }
}

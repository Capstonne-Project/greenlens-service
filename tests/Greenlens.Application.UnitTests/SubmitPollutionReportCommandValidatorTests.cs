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
            TempImageId: null,
            Images:
            [
                new SubmitPollutionReportImageItem(
                    "https://cdn.example.com/a.jpg",
                    "image/jpeg",
                    100)
            ],
            WasteTagIds: null);

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
            TempImageId: Guid.NewGuid().ToString("N"),
            Images: null,
            WasteTagIds: null);

    // ── Direct-R2 images with optional AI analysis ───────────────────────

    [Fact]
    public void Validate_DirectR2ImagesWithAnalysis_IsValid_BR_AI_001()
    {
        var cmd = ManualBase() with { TempImageId = Guid.NewGuid().ToString("N") };
        Assert.True(_sut.Validate(cmd).IsValid);
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

    // ── Description BR-REP-004 ────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyDescription_IsValid_BR_REP_004()
    {
        Assert.True(_sut.Validate(ManualBase()).IsValid);
    }

    [Fact]
    public void Validate_DescriptionTooShort_IsInvalid_BR_REP_004()
    {
        var cmd = ManualBase() with { Description = "ngắn" };
        Assert.False(_sut.Validate(cmd).IsValid);
    }

    [Fact]
    public void Validate_DescriptionMinLength_IsValid_BR_REP_004()
    {
        var cmd = ManualBase() with { Description = "1234567890" };
        Assert.True(_sut.Validate(cmd).IsValid);
    }
}

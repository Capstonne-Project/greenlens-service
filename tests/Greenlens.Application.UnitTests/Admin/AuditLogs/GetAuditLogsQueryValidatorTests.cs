using Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogs;

namespace Greenlens.Application.UnitTests.Admin.AuditLogs;

public sealed class GetAuditLogsQueryValidatorTests
{
    private readonly GetAuditLogsQueryValidator _sut = new();

    [Fact]
    public void Validate_DefaultQuery_IsValid_BR_ADM_010()
    {
        var result = _sut.Validate(new GetAuditLogsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeOver100_IsInvalid_BR_ADM_010()
    {
        var result = _sut.Validate(new GetAuditLogsQuery(PageSize: 101));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_FromDateAfterToDate_IsInvalid_BR_ADM_010()
    {
        var result = _sut.Validate(new GetAuditLogsQuery(
            FromDate: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            ToDate: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));

        Assert.False(result.IsValid);
    }
}

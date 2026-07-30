using Greenlens.Application.Features.Admin.AuditLogs.ExportAuditLogs;

namespace Greenlens.Application.UnitTests.Admin.AuditLogs;

public sealed class ExportAuditLogsQueryValidatorTests
{
    private readonly ExportAuditLogsQueryValidator _sut = new();

    [Fact]
    public void Validate_MissingDateRange_IsInvalid_BR_ADM_010()
    {
        var result = _sut.Validate(new ExportAuditLogsQuery(default, default));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RangeOver90Days_IsInvalid_BR_ADM_010()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(91);

        var result = _sut.Validate(new ExportAuditLogsQuery(from, to));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ValidRange_IsValid_BR_ADM_010()
    {
        var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.Validate(new ExportAuditLogsQuery(from, to));

        Assert.True(result.IsValid);
    }
}

using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class ReportFlagTests
{
    [Fact]
    public void Create_Duplicate_SetsFields_BR_REP_033()
    {
        var reportId = Guid.NewGuid();
        var flaggerId = Guid.NewGuid();

        var flag = ReportFlag.Create(reportId, flaggerId, FlagType.Duplicate, "Trùng với báo cáo khác");

        Assert.Equal(reportId, flag.ReportId);
        Assert.Equal(flaggerId, flag.FlaggerId);
        Assert.Equal(FlagType.Duplicate, flag.FlagType);
        Assert.Equal("Trùng với báo cáo khác", flag.Reason);
        Assert.NotEqual(default, flag.CreatedAt);
    }

    [Fact]
    public void Create_WithoutReason_AllowsNull_BR_REP_033()
    {
        var flag = ReportFlag.Create(Guid.NewGuid(), Guid.NewGuid(), FlagType.Spam);

        Assert.Null(flag.Reason);
        Assert.Equal(FlagType.Spam, flag.FlagType);
    }
}

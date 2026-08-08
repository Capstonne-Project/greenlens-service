using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class ReportReopenRequestTests
{
    [Fact]
    public void Create_WithValidReason_ShouldSetPending_BR_REP_015()
    {
        var request = ReportReopenRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "Vẫn còn rác ở góc hẻm sau khi dọn.");

        Assert.Equal(ReopenRequestStatus.Pending, request.Status);
        Assert.Equal("Vẫn còn rác ở góc hẻm sau khi dọn.", request.Reason);
    }

    [Fact]
    public void Reject_FromPending_ShouldSetRejectedReason_BR_REP_022()
    {
        var request = ReportReopenRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "Vẫn còn rác ở góc hẻm sau khi dọn.");
        var leoId = Guid.NewGuid();

        request.Reject(leoId, "Ảnh không khớp vị trí báo cáo ban đầu.");

        Assert.Equal(ReopenRequestStatus.Rejected, request.Status);
        Assert.Equal(leoId, request.ReviewedBy);
        Assert.NotNull(request.RejectionReason);
    }
}

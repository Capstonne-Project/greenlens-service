using FluentAssertions;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class NotificationTemplateRendererTests
{
    [Fact]
    public void Render_CanonicalPlaceholders_ReplacesReportCodeAndStatus_BR_NTF_002()
    {
        var placeholders = NotificationPlaceholders.ForReportStatus("RPT-2608", ReportStatus.InProgress);
        var template = "Báo cáo {report_code} của bạn đã chuyển sang trạng thái: {status}.";

        var rendered = NotificationTemplateRenderer.Render(template, placeholders);

        rendered.Should().Be("Báo cáo RPT-2608 của bạn đã chuyển sang trạng thái: Đang xử lý.");
    }

    [Fact]
    public void Render_LegacyDoubleBraceReportCode_ReplacesValue_BR_NTF_002()
    {
        var placeholders = NotificationPlaceholders.ForReportStatus("RPT-2608", ReportStatus.Verified);
        var template = "Báo cáo {{ReportCode}} đã thay đổi trạng thái thành {{Status}}.";

        var rendered = NotificationTemplateRenderer.Render(template, placeholders);

        rendered.Should().Be("Báo cáo RPT-2608 đã thay đổi trạng thái thành Đã xác minh.");
    }

    [Fact]
    public void Render_LegacyPascalCasePlaceholders_ReplacesValue_BR_NTF_002()
    {
        var placeholders = NotificationPlaceholders.ForReportStatus("RPT-99", ReportStatus.Resolved);
        var template = "Báo cáo {ReportCode} đã chuyển sang {Status}.";

        var rendered = NotificationTemplateRenderer.Render(template, placeholders);

        rendered.Should().Be("Báo cáo RPT-99 đã chuyển sang Đã xử lý xong.");
    }

    [Fact]
    public void ContainsLegacyPlaceholders_DetectsDoubleBraceSyntax()
    {
        NotificationTemplateRenderer.ContainsLegacyPlaceholders("Báo cáo {{ReportCode}} đã thay đổi trạng thái.")
            .Should().BeTrue();
    }

    [Fact]
    public void ContainsLegacyPlaceholders_IgnoresCanonicalSyntax()
    {
        NotificationTemplateRenderer.ContainsLegacyPlaceholders("Báo cáo {report_code} đã chuyển sang {status}.")
            .Should().BeFalse();
    }
}

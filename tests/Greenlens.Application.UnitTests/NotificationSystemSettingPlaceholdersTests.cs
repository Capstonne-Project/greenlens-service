using FluentAssertions;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.UnitTests.TestDoubles;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class NotificationSystemSettingPlaceholdersTests
{
    [Fact]
    public void Merge_UsesDefaultFallbackValues_BR_NTF_002()
    {
        var merged = NotificationSystemSettingPlaceholders.Merge(
            new Dictionary<string, string> { ["report_code"] = "RPT-001" },
            new DefaultSystemSettingsProvider());

        merged["report_code"].Should().Be("RPT-001");
        merged["sla_verify_hours"].Should().Be("24");
        merged["overdue_pending_hours"].Should().Be("72");
        merged["auto_close_resolved_days"].Should().Be("2");
        merged["nearby_radius_km"].Should().Be("2");
        merged["progress_escalate_hours"].Should().Be("48");
        merged["invitation_response_days"].Should().Be("7");
        merged["check_in_reminder_minutes"].Should().Be("15");
        merged["duplicate_radius_meters"].Should().Be("25");
    }

    [Fact]
    public void Render_ReportAutoClosedTemplate_UsesConfigPlaceholder_BR_REP_016()
    {
        var merged = NotificationSystemSettingPlaceholders.Merge(
            new Dictionary<string, string> { ["report_code"] = "RPT-99" },
            new DefaultSystemSettingsProvider());

        const string template =
            "Báo cáo {report_code} đã được hệ thống tự động đóng sau {auto_close_resolved_days} ngày chờ xác nhận.";

        var rendered = NotificationTemplateRenderer.Render(template, merged);

        rendered.Should().Be("Báo cáo RPT-99 đã được hệ thống tự động đóng sau 2 ngày chờ xác nhận.");
    }

    [Fact]
    public void ForDuplicateReviewFromTier1Geo_UsesConfiguredRadius_BR_REP_030()
    {
        var placeholders = NotificationPlaceholders.ForDuplicateReviewFromTier1Geo(
            "RPT-A",
            "RPT-B",
            duplicateRadiusMeters: 40);

        placeholders["detection_summary"].Should().Contain("40m");
    }
}

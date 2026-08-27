using System.Globalization;
using Greenlens.Application.Common.Options;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.CommunityCleanup.Common;

internal static class CommunityCleanupShareBuilder
{
    private static readonly string[] DefaultHashtags = ["GreenLens", "DonDepCongDong"];
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

    public static CommunityCleanupShareDto Build(
        CommunityCleanupEvent ev,
        Report report,
        string? imageUrl,
        PublicWebOptions publicWeb)
    {
        var url = publicWeb.BuildCommunityCleanupUrl(ev.Id);
        var caption = BuildCaption(ev, report, url);
        var encodedUrl = Uri.EscapeDataString(url);
        var encodedCaption = Uri.EscapeDataString(caption);

        return new CommunityCleanupShareDto(
            Url: url,
            Caption: caption,
            ImageUrl: imageUrl,
            FacebookShareUrl: $"https://www.facebook.com/sharer/sharer.php?u={encodedUrl}",
            TwitterShareUrl: $"https://twitter.com/intent/tweet?text={encodedCaption}",
            LinkedInShareUrl: $"https://www.linkedin.com/sharing/share-offsite/?url={encodedUrl}",
            Hashtags: DefaultHashtags);
    }

    private static string BuildCaption(CommunityCleanupEvent ev, Report report, string url)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var startsLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(ev.StartsAt, DateTimeKind.Utc),
            VietnamTimeZone);

        var lines = new List<string> { $"🌿 {ev.Title}" };

        if (!string.IsNullOrWhiteSpace(ev.Description))
            lines.Add(ev.Description);

        if (!string.IsNullOrWhiteSpace(report.Address))
            lines.Add($"📍 {report.Address}");

        if (!string.IsNullOrWhiteSpace(ev.MeetingNote))
            lines.Add($"ℹ️ {ev.MeetingNote}");

        lines.Add($"🕐 {startsLocal.ToString("dd/MM/yyyy HH:mm", vi)}");
        lines.Add(string.Empty);
        lines.Add($"Tham gia tại: {url}");
        lines.Add("#GreenLens #DonDepCongDong");

        return string.Join('\n', lines);
    }
}

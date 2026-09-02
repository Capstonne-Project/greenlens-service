using System.Globalization;
using System.Text;
using Greenlens.Application.Common;
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
        var caption = BuildCaption(ev, report, publicWeb);
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

    private static string BuildCaption(CommunityCleanupEvent ev, Report report, PublicWebOptions publicWeb)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var sb = new StringBuilder();

        sb.AppendLine($"🌱 THÔNG TIN THAM GIA {ev.Title.ToUpper(vi)} CÙNG GREENLENS 🌱");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(ev.Description))
        {
            sb.AppendLine("🎒 Chuẩn bị cá nhân");
            foreach (var line in FormatDescriptionLines(ev.Description))
                sb.AppendLine(line);
            sb.AppendLine();
        }

        sb.AppendLine("⏰ Lịch trình hoạt động");
        sb.Append("• Ngày bắt đầu: ");
        sb.AppendLine(FormatScheduleDateTime(ev.StartsAt, vi));

        if (ev.EndsAt.HasValue)
        {
            sb.Append("• Ngày kết thúc: ");
            sb.AppendLine(FormatScheduleDateTime(ev.EndsAt.Value, vi));
        }

        sb.AppendLine();

        sb.AppendLine("📍 Địa điểm tập trung");
        if (!string.IsNullOrWhiteSpace(report.Address))
            sb.AppendLine(report.Address.Trim());

        if (!string.IsNullOrWhiteSpace(ev.MeetingNote))
            sb.AppendLine(ev.MeetingNote.Trim());

        sb.AppendLine();
        sb.AppendLine("👉 Khi đến nơi, bạn vui lòng:");
        sb.AppendLine("• Check-in theo hướng dẫn của Leader");
        sb.AppendLine("• Tuân thủ hướng dẫn an toàn tại khu vực");
        sb.AppendLine();
        sb.AppendLine("⚠️ Lưu ý quan trọng");
        sb.AppendLine("• Thời gian hoạt động có thể thay đổi tùy theo khối lượng rác thực tế");
        sb.AppendLine("• Hãy tuân thủ hướng dẫn an toàn và phối hợp cùng đội nhóm để đạt hiệu quả cao nhất");
        sb.AppendLine();
        // Markdown link — label "GreenLens", href PublicWeb:BaseUrl; FB post strip sang plain + URL.
        var portalLink = MarkdownPlainText.FormatLink("GreenLens", publicWeb.BaseUrl);
        sb.AppendLine($"📲 Tải ứng dụng tại {portalLink} để cùng chung tay bảo vệ môi trường nhé!");
        sb.AppendLine();
        sb.AppendLine("📞 Hỗ trợ & liên hệ");
        sb.AppendLine();
        sb.AppendLine("098 773 0708");
        sb.AppendLine();
        sb.Append("#GreenLens #DonDepCongDong #CaiNhinDonDep #ChamSocMoiTruong");

        return NormalizeLineEndings(sb.ToString());
    }

    /// <summary>
    /// Ensures Unix newlines for Facebook/social captions (avoids \r\n collapsing on some clients).
    /// </summary>
    internal static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').TrimEnd();

    /// <summary>
    /// Formats description: plain single line stays as-is; newlines or markdown/list markers become • bullets.
    /// </summary>
    private static IEnumerable<string> FormatDescriptionLines(string description)
    {
        var plain = MarkdownPlainText.ToPlain(description);
        var lines = plain
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
            yield break;

        var useBullets = lines.Count > 1 || lines.Any(IsExplicitListMarkerLine);

        if (!useBullets)
        {
            yield return lines[0];
            yield break;
        }

        foreach (var line in lines)
        {
            if (line.StartsWith('•'))
            {
                yield return line.StartsWith("• ", StringComparison.Ordinal) ? line : "• " + line[1..].TrimStart();
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                yield return "• " + line[2..].TrimStart();
                continue;
            }

            yield return "• " + line;
        }
    }

    private static bool IsExplicitListMarkerLine(string line) =>
        line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith('•');

    private static string FormatScheduleDateTime(DateTime utc, CultureInfo vi)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            VietnamTimeZone);

        var dayName = vi.DateTimeFormat.GetDayName(local.DayOfWeek);
        return $"{local:HH:mm} {dayName}, {local:dd/MM/yyyy}";
    }
}

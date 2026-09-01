namespace Greenlens.Application.Features.Reports.Common;

/// <summary>
/// Mobile gửi lat/lng từ EXIF ảnh; (0,0) nghĩa là chưa có tọa độ — BE sẽ đọc EXIF trong file R2.
/// </summary>
internal static class ProgressUpdateCoordinates
{
    public static bool IsMissing(decimal latitude, decimal longitude) =>
        latitude == 0m && longitude == 0m;
}

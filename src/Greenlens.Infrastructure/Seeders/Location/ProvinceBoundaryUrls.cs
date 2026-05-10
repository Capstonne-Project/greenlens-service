namespace Greenlens.Infrastructure.Persistence.Seeders.Location;

/// <summary>
/// URL tới GeoJSON polygon ranh giới của 34 tỉnh/thành (CDN).
/// Phục vụ <c>BR-MAP-*</c> cho việc render map ở FE.
/// </summary>
/// <remarks>
/// Các URL này trỏ tới CDN public của project location-service trước đây. 
/// Khi go-live, team cần thay bằng CDN của Greenlens (Cloudflare R2 + custom domain — xem §14.2 CLAUDE.md).
/// </remarks>
internal static class ProvinceBoundaryUrls
{
    private const string CdnBase = "https://d3iova6424vljy.cloudfront.net/prod/location/boundaries";

    public static IReadOnlyDictionary<string, string> Mapping { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["01"] = $"{CdnBase}/ha_noi.json",
            ["04"] = $"{CdnBase}/cao_bang.json",
            ["08"] = $"{CdnBase}/tuyen_quang.json",
            ["11"] = $"{CdnBase}/dien_bien.json",
            ["12"] = $"{CdnBase}/lai_chau.json",
            ["14"] = $"{CdnBase}/son_la.json",
            ["15"] = $"{CdnBase}/lao_cai.json",
            ["19"] = $"{CdnBase}/thai_nguyen.json",
            ["20"] = $"{CdnBase}/lang_son.json",
            ["22"] = $"{CdnBase}/quang_ninh.json",
            ["24"] = $"{CdnBase}/bac_ninh.json",
            ["25"] = $"{CdnBase}/phu_tho.json",
            ["31"] = $"{CdnBase}/hai_phong.json",
            ["33"] = $"{CdnBase}/hung_yen.json",
            ["37"] = $"{CdnBase}/ninh_binh.json",
            ["38"] = $"{CdnBase}/thanh_hoa.json",
            ["40"] = $"{CdnBase}/nghe_an.json",
            ["42"] = $"{CdnBase}/ha_tinh.json",
            ["44"] = $"{CdnBase}/quang_tri.json",
            ["46"] = $"{CdnBase}/hue.json",
            ["48"] = $"{CdnBase}/da_nang.json",
            ["51"] = $"{CdnBase}/quang_ngai.json",
            ["52"] = $"{CdnBase}/gia_lai.json",
            ["56"] = $"{CdnBase}/khanh_hoa.json",
            ["66"] = $"{CdnBase}/dak_lak.json",
            ["68"] = $"{CdnBase}/lam_dong.json",
            ["75"] = $"{CdnBase}/dong_nai.json",
            ["79"] = $"{CdnBase}/tp_ho_chi_minh.json",
            ["80"] = $"{CdnBase}/tay_ninh.json",
            ["82"] = $"{CdnBase}/dong_thap.json",
            ["86"] = $"{CdnBase}/vinh_long.json",
            ["91"] = $"{CdnBase}/an_giang.json",
            ["92"] = $"{CdnBase}/can_tho.json",
            ["96"] = $"{CdnBase}/ca_mau.json",
        };
}

namespace Greenlens.Application.Common;

/// <summary>
/// Ward + province equality for geo proximity checks (BR-REP-030, BR-REP-034).
/// Prevents false positives when GPS points straddle an administrative boundary
/// (e.g. opposite sides of the same street in different wards).
/// </summary>
public static class AdministrativeUnitMatch
{
    public static bool HasWardAndProvince(string? wardCode, string? provinceCode) =>
        !string.IsNullOrWhiteSpace(wardCode) && !string.IsNullOrWhiteSpace(provinceCode);

    public static bool SameWardAndProvince(
        string? wardCode,
        string? provinceCode,
        string? otherWardCode,
        string? otherProvinceCode) =>
        HasWardAndProvince(wardCode, provinceCode)
        && HasWardAndProvince(otherWardCode, otherProvinceCode)
        && wardCode == otherWardCode
        && provinceCode == otherProvinceCode;
}

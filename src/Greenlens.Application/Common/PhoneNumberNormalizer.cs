namespace Greenlens.Application.Common;

/// <summary>Normalizes VN phone numbers to international format without '+' (84xxxxxxxxx).</summary>
public static class PhoneNumberNormalizer
{
    public static string? Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+84", StringComparison.Ordinal))
            return phone[1..];
        if (phone.StartsWith("84", StringComparison.Ordinal) && phone.Length >= 11)
            return phone;
        if (phone.StartsWith('0'))
            return "84" + phone[1..];

        return phone;
    }
}

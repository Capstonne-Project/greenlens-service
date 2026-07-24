using System.Text.RegularExpressions;

namespace Greenlens.Application.Common;

/// <summary>Normalizes VN phone numbers to international format without '+' (84xxxxxxxxx).</summary>
public static partial class PhoneNumberNormalizer
{
    // Matches Register/Login validators: 84 + 8–10 subscriber digits.
    [GeneratedRegex(@"^84[0-9]{8,10}$")]
    private static partial Regex NormalizedVnPhone();

    public static string? Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        phone = phone.Trim().Replace(" ", "").Replace("-", "");

        string? normalized = phone switch
        {
            _ when phone.StartsWith("+84", StringComparison.Ordinal) => phone[1..],
            _ when phone.StartsWith("84", StringComparison.Ordinal) => phone,
            _ when phone.StartsWith('0') => "84" + phone[1..],
            _ => null
        };

        if (normalized is null || !NormalizedVnPhone().IsMatch(normalized))
            return null;

        return normalized;
    }
}

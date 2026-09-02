using System.Text;

namespace Greenlens.Application.Common;

/// <summary>Chuẩn hoá và token hoá chuỗi search tiếng Việt cho PostgreSQL ILike.</summary>
internal static class VietnameseTextSearch
{
    /// <summary>
    /// Trim, NFC, tách theo khoảng trắng và escape wildcard ILike (% _ \).
    /// </summary>
    public static IReadOnlyList<string> Tokenize(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return [];

        return search.Trim()
            .Normalize(NormalizationForm.FormC)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(EscapeLikePattern)
            .ToArray();
    }

    public static string ToContainsPattern(string token) => $"%{token}%";

    private static string EscapeLikePattern(string token) =>
        token.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}

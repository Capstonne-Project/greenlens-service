namespace Greenlens.Application.Common.Mappings;

/// <summary>
/// Maps AI Service pollution class labels to stable <c>pollution_categories.code</c> values.
/// </summary>
/// <remarks>Implements: BR-AI-001 (classification hint for report form).</remarks>
public static class AiPollutionClassMapper
{
    public static string? ToCategoryCode(string? aiClass)
    {
        if (string.IsNullOrWhiteSpace(aiClass))
            return null;

        return aiClass.Trim().ToUpperInvariant() switch
        {
            "TRASH" => "TRASH",
            "WATER" => "WASTEWATER",
            "SMOKE" => "SMOKE",
            "CHEMICAL" => "CHEMICAL",
            _ => null
        };
    }
}

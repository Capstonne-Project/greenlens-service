namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Finds citizens to notify about nearby pollution reports (BR-NTF-002, BR-MAP-002).
/// Uses prior report locations as a proxy until dedicated user location is stored.
/// </summary>
public interface INearbyCitizenQuery
{
    /// <summary>
    /// Returns distinct citizen user IDs who have submitted at least one report
    /// within <paramref name="radiusMeters"/> of the given point.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindCitizenIdsWithinRadiusAsync(
        decimal latitude,
        decimal longitude,
        Guid? excludeUserId,
        double radiusMeters,
        int maxRecipients,
        CancellationToken ct = default);
}

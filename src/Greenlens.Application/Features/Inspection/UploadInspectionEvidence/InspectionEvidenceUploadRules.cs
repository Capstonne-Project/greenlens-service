using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Inspection.UploadInspectionEvidence;

internal static class InspectionEvidenceUploadRules
{
    internal const int MaxItemsPerRequest = 5;

    internal const long MaxImageBytes = 20 * 1024 * 1024;
    internal const long MaxVideoBytes = 30 * 1024 * 1024;
    internal const long MaxAudioBytes = 10 * 1024 * 1024;

    internal static long MaxBytesFor(InspectionEvidenceCategory category) =>
        category switch
        {
            InspectionEvidenceCategory.Video => MaxVideoBytes,
            InspectionEvidenceCategory.Audio => MaxAudioBytes,
            _ => MaxImageBytes
        };

    internal static string BuildFolderPrefix(Guid reportId, Guid inspectionId, InspectionEvidenceCategory category)
        => $"reports/{reportId}/inspection/{inspectionId}/{category.ToString().ToLowerInvariant()}";

    internal static bool UrlMatchesFolder(string url, string folderPrefix)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        return uri.AbsolutePath.Contains(folderPrefix, StringComparison.OrdinalIgnoreCase);
    }
}

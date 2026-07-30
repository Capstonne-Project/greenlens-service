using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Validates hardcoded inspection checklist requirements (BR-INS-033).</summary>
public static class InspectionChecklistValidator
{
    public static Error? Validate(IReadOnlyList<InspectionEvidence> evidences)
    {
        var violationStatus = evidences
            .FirstOrDefault(e => e.Category == InspectionEvidenceCategory.ViolationStatus);

        if (string.IsNullOrWhiteSpace(violationStatus?.Description))
        {
            return Errors.Inspections.ChecklistViolationStatusRequired;
        }

        var scenePhotoCount = evidences.Count(e =>
            e.Category == InspectionEvidenceCategory.ScenePhoto
            && !string.IsNullOrWhiteSpace(e.MediaUrl));

        if (scenePhotoCount < 2)
        {
            return Errors.Inspections.InsufficientEvidenceImages;
        }

        return null;
    }
}

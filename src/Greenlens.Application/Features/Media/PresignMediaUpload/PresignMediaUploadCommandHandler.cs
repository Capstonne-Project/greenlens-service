using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Inspection;
using Greenlens.Application.Features.Inspection.UploadInspectionEvidence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Media.PresignMediaUpload;

/// <summary>
/// Create a presigned R2 PUT URL for direct client upload.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (allowed image MIME), BR-SYS-002 (object storage), BR-REP-015 (reopen evidence guard).
/// </remarks>
public sealed class PresignMediaUploadCommandHandler(
    IFileStorageService fileStorage,
    IReportRepository reports,
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    ISystemSettingsProvider systemSettings,
    ILogger<PresignMediaUploadCommandHandler> logger)
    : IRequestHandler<PresignMediaUploadCommand, Result<PresignMediaUploadResponse>>
{
    public async Task<Result<PresignMediaUploadResponse>> Handle(
        PresignMediaUploadCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting presign media upload");

        if (!CanUploadPurpose(currentUser.Role, request.Purpose))
        {
            logger.LogWarning("Upload purpose {Purpose} is forbidden", request.Purpose);
            return Errors.Media.UploadPurposeForbidden;
        }

        if (request.Purpose is MediaUploadPurpose.ReopenEvidence)
        {
            var reopenGuardError = await ValidateReopenEvidenceUploadAsync(request.ReportId, cancellationToken)
                .ConfigureAwait(false);
            if (reopenGuardError is not null)
            {
                logger.LogWarning(
                    "Reopen evidence presign blocked: {ErrorCode}, ReportId={ReportId}",
                    reopenGuardError.Code,
                    request.ReportId);
                return reopenGuardError;
            }
        }

        if (request.Purpose is MediaUploadPurpose.InspectionEvidence)
        {
            var inspectionGuardError = await ValidateInspectionEvidenceUploadAsync(
                    request.InspectionId!.Value,
                    request.EvidenceCategory!.Value,
                    cancellationToken)
                .ConfigureAwait(false);
            if (inspectionGuardError is not null)
            {
                logger.LogWarning(
                    "Inspection evidence presign blocked: {ErrorCode}, InspectionId={InspectionId}",
                    inspectionGuardError.Code,
                    request.InspectionId);
                return inspectionGuardError;
            }
        }

        if (!TryResolveLimits(request, out var folderTemplate, out var maxBytes, out var requireImageMime))
        {
            logger.LogWarning("Invalid upload purpose {Purpose}", request.Purpose);
            return Errors.Media.InvalidUploadPurpose;
        }
        var safeFileName = Path.GetFileName(request.FileName.Trim());
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            logger.LogWarning("Invalid file name {FileName}", request.FileName);
            return Errors.Media.InvalidFileName;
        }

        string contentType;
        if (requireImageMime)
        {
            if (!ReportImageContentTypes.TryResolve(safeFileName, request.ContentType, out contentType))
            {
                logger.LogWarning("Invalid image type {ContentType}", request.ContentType);
                return Errors.Media.InvalidImageType;
            }
        }
        else if (request.Purpose is MediaUploadPurpose.InspectionEvidence)
        {
            contentType = request.ContentType.Trim();
            if (!IsAllowedInspectionEvidenceContentType(request.EvidenceCategory!.Value, safeFileName, contentType))
            {
                logger.LogWarning(
                    "Invalid content type {ContentType} for inspection evidence category {Category}",
                    contentType,
                    request.EvidenceCategory);
                return request.EvidenceCategory switch
                {
                    InspectionEvidenceCategory.Video => Errors.Media.InvalidVideoType,
                    InspectionEvidenceCategory.Audio => Errors.Media.InvalidImageType,
                    _ => Errors.Media.InvalidImageType
                };
            }
        }
        else
        {
            contentType = request.ContentType.Trim();
        }

        if (request.FileSizeBytes is > 0 and var size && size > maxBytes)
        {
            logger.LogWarning("Image too large {Size} bytes", size);
            return Errors.Media.ImageTooLarge;
        }

        var folderReportId = request.ReportId;
        if (request.Purpose is MediaUploadPurpose.InspectionEvidence)
        {
            var inspection = await inspections.GetByIdAsync(request.InspectionId!.Value, cancellationToken)
                .ConfigureAwait(false);
            folderReportId = inspection!.ReportId;
        }

        var folder = folderTemplate
            .Replace("{reportId}", folderReportId?.ToString() ?? "unknown", StringComparison.Ordinal)
            .Replace("{inspectionId}", request.InspectionId?.ToString() ?? "unknown", StringComparison.Ordinal)
            .Replace("{category}", request.EvidenceCategory?.ToString().ToLowerInvariant() ?? "unknown", StringComparison.Ordinal);

        try
        {
            var presignTtlMinutes = ModuleSystemSettings.Ai(systemSettings).PresignUploadTtlMinutes;
            var presignTtl = TimeSpan.FromMinutes(Math.Max(1, presignTtlMinutes));

            var signed = await fileStorage.CreatePresignedUploadAsync(
                    safeFileName,
                    contentType,
                    folder,
                    presignTtl,
                    cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Presigned media upload created. Purpose={Purpose}, Key={Key}, MaxBytes={MaxBytes}",
                request.Purpose, signed.Key, maxBytes);

            return new PresignMediaUploadResponse(
                signed.UploadUrl,
                signed.PublicUrl,
                signed.Key,
                signed.ContentType,
                RequiredHeaders: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Content-Type"] = signed.ContentType
                },
                signed.ExpiresInSeconds,
                maxBytes,
                request.Purpose);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create presigned upload for purpose {Purpose}", request.Purpose);
            return Errors.Users.StorageUploadFailed;
        }
    }

    private static bool TryResolveLimits(
        PresignMediaUploadCommand request,
        out string folderTemplate,
        out long maxBytes,
        out bool requireImageMime)
    {
        requireImageMime = true;
        switch (request.Purpose)
        {
            case MediaUploadPurpose.ReportImage:
            case MediaUploadPurpose.After:
                folderTemplate = "reports/images";
                maxBytes = 10 * 1024 * 1024;
                return true;
            case MediaUploadPurpose.Before:
                folderTemplate = "reports/{reportId}/before";
                maxBytes = 20 * 1024 * 1024;
                return true;
            case MediaUploadPurpose.Progress:
                folderTemplate = "reports/{reportId}/progress";
                maxBytes = 20 * 1024 * 1024;
                return true;
            case MediaUploadPurpose.Comment:
                folderTemplate = "comments/images";
                maxBytes = 5 * 1024 * 1024;
                return true;
            case MediaUploadPurpose.Avatar:
                folderTemplate = "users/avatars";
                maxBytes = 5 * 1024 * 1024;
                return true;
            case MediaUploadPurpose.ReopenEvidence:
                folderTemplate = "reports/{reportId}/reopen";
                maxBytes = 10 * 1024 * 1024;
                return true;
            case MediaUploadPurpose.InspectionEvidence:
                folderTemplate = "reports/{reportId}/inspection/{inspectionId}/{category}";
                maxBytes = InspectionEvidenceUploadRules.MaxBytesFor(request.EvidenceCategory!.Value);
                requireImageMime = request.EvidenceCategory is InspectionEvidenceCategory.ScenePhoto
                    or InspectionEvidenceCategory.Other;
                return true;
            default:
                folderTemplate = string.Empty;
                maxBytes = 0;
                return false;
        }
    }

    private static bool CanUploadPurpose(string role, MediaUploadPurpose purpose)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return true;

        var normalizedRole = role.Trim().ToUpperInvariant();
        return purpose switch
        {
            MediaUploadPurpose.Before or MediaUploadPurpose.After =>
                normalizedRole is "CLEANER" or "COMPANYSTAFF",
            MediaUploadPurpose.Progress =>
                normalizedRole is "CLEANER" or "COMPANYSTAFF" or "INSPECTOR",
            MediaUploadPurpose.InspectionEvidence =>
                normalizedRole is "INSPECTOR",
            MediaUploadPurpose.ReportImage or
            MediaUploadPurpose.Comment or
            MediaUploadPurpose.Avatar or
            MediaUploadPurpose.ReopenEvidence => true,
            _ => false
        };
    }

    private async Task<Error?> ValidateReopenEvidenceUploadAsync(Guid? reportId, CancellationToken ct)
    {
        if (!reportId.HasValue || reportId.Value == Guid.Empty)
        {
            logger.LogWarning("Report ID is required for reopen evidence upload");
            return Errors.Reports.ReportNotFound;
        }

        var report = await reports.GetByIdAsync(reportId.Value, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", reportId.Value);
            return Errors.Reports.ReportNotFound;
        }

        if (report.ReporterId != currentUser.UserId)
        {
            logger.LogWarning("Report {ReportId} is not owned by user {UserId}", report.Id, currentUser.UserId);
            return Errors.Reports.NotReporter;
        }

        return ReopenRequestEligibility.ValidateCitizenCanRequest(
            report,
            DateTime.UtcNow,
            ReportSystemSettings.ReopenWindowDays(systemSettings),
            ReportSystemSettings.MaxApprovedReopens(systemSettings));
    }

    private async Task<Error?> ValidateInspectionEvidenceUploadAsync(
        Guid inspectionId,
        InspectionEvidenceCategory category,
        CancellationToken ct)
    {
        if (category is InspectionEvidenceCategory.ViolationStatus)
            return Errors.Inspections.ChecklistViolationStatusRequired;

        var inspection = await inspections.GetByIdAsync(inspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        if (inspection.FieldInvestigationSubmittedAt.HasValue)
            return Errors.Inspections.FieldReportAlreadySubmitted;

        if (inspection.Status != InspectionStatus.InProgress)
            return Errors.Inspections.InvalidStatusTransition;

        return await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection,
            teamMembers,
            currentUser,
            ct).ConfigureAwait(false);
    }

    private static bool IsAllowedInspectionEvidenceContentType(
        InspectionEvidenceCategory category,
        string fileName,
        string contentType)
    {
        return category switch
        {
            InspectionEvidenceCategory.ScenePhoto or InspectionEvidenceCategory.Other =>
                ReportImageContentTypes.IsAllowed(fileName, contentType),
            InspectionEvidenceCategory.Video =>
                contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetExtension(fileName), ".mp4", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetExtension(fileName), ".mov", StringComparison.OrdinalIgnoreCase),
            InspectionEvidenceCategory.Audio =>
                contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}

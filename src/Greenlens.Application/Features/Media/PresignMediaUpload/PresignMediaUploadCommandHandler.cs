using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
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
    ICurrentUser currentUser,
    ILogger<PresignMediaUploadCommandHandler> logger)
    : IRequestHandler<PresignMediaUploadCommand, Result<PresignMediaUploadResponse>>
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

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

        if (!TryResolveLimits(request.Purpose, out var folderTemplate, out var maxBytes, out var requireImageMime))
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
        else
        {
            contentType = request.ContentType.Trim();
        }

        if (request.FileSizeBytes is > 0 and var size && size > maxBytes)
        {
            logger.LogWarning("Image too large {Size} bytes", size);
            return Errors.Media.ImageTooLarge;
        }

        var folder = folderTemplate
            .Replace("{reportId}", request.ReportId?.ToString("N") ?? "unknown", StringComparison.Ordinal);

        try
        {
            var signed = await fileStorage.CreatePresignedUploadAsync(
                    safeFileName,
                    contentType,
                    folder,
                    DefaultTtl,
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
        MediaUploadPurpose purpose,
        out string folderTemplate,
        out long maxBytes,
        out bool requireImageMime)
    {
        requireImageMime = true;
        switch (purpose)
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

        return ReopenRequestEligibility.ValidateCitizenCanRequest(report, DateTime.UtcNow);
    }
}

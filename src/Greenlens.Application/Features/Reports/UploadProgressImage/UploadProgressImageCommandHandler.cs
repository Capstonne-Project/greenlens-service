using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.UploadProgressImage;

/// <summary>
/// Upload a progress image (mid-task) to cloud storage and return the URL.
/// Assignment must be InProgress. The returned URL is used in UpdateProgressCommand.
/// </summary>
public sealed class UploadProgressImageCommandHandler(
    IReportAssignmentRepository assignments,
    IFileStorageService fileStorage,
    ILogger<UploadProgressImageCommandHandler> logger) : IRequestHandler<UploadProgressImageCommand, Result<UploadProgressImageResponse>>
{
    public async Task<Result<UploadProgressImageResponse>> Handle(
        UploadProgressImageCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Uploading progress image for report {ReportId} team {TeamId}",
            request.ReportId, request.TeamId);

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);

        if (assignment is null)
        {
            logger.LogWarning("Assignment not found for report {ReportId} and team {TeamId}", request.ReportId, request.TeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        if (assignment.Status != AssignmentStatus.InProgress)
        {
            logger.LogWarning("Assignment {AssignmentId} is not in a valid status for progress image upload", assignment.Id);
            return Errors.Reports.AssignmentNotInProgress;
        }

        var folder = $"reports/{request.ReportId}/progress/{request.TeamId}";
        using var stream = new MemoryStream(request.ImageBytes);
        var uploaded = await fileStorage.UploadAsync(
            stream, request.FileName, request.ContentType, folder, ct).ConfigureAwait(false);

        logger.LogInformation("Progress image uploaded for report {ReportId} team {TeamId}",
            request.ReportId, request.TeamId);

        return new UploadProgressImageResponse(uploaded.Url);
    }
}

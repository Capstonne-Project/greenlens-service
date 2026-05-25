using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.UploadProgressImage;

/// <summary>
/// Upload a progress image (mid-task) to cloud storage and return the URL.
/// Assignment must be InProgress. The returned URL is used in UpdateProgressCommand.
/// </summary>
public sealed class UploadProgressImageCommandHandler(
    IReportAssignmentRepository assignments,
    IFileStorageService fileStorage,
    IUnitOfWork uow) : IRequestHandler<UploadProgressImageCommand, Result<UploadProgressImageResponse>>
{
    public async Task<Result<UploadProgressImageResponse>> Handle(
        UploadProgressImageCommand request,
        CancellationToken ct)
    {
        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.InProgress)
            return Errors.Reports.AssignmentNotInProgress;

        var folder = $"reports/{request.ReportId}/progress/{request.TeamId}";
        using var stream = new MemoryStream(request.ImageBytes);
        var uploaded = await fileStorage.UploadAsync(
            stream, request.FileName, request.ContentType, folder, ct).ConfigureAwait(false);

        return new UploadProgressImageResponse(uploaded.Url);
    }
}

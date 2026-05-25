using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.UpdateProgress;

/// <summary>
/// Team leader updates cleanup progress (percent, note, optional images).
/// TeamId resolved from JWT token — caller must be a team leader.
/// Does NOT change report or assignment status.
/// </summary>
public sealed class UpdateProgressCommandHandler(
    IReportAssignmentRepository assignments,
    ITeamMemberRepository teamMembers,
    IFileStorageService fileStorage,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<UpdateProgressCommand, Result<UpdateProgressResponse>>
{
    public async Task<Result<UpdateProgressResponse>> Handle(UpdateProgressCommand request, CancellationToken ct)
    {
        if (request.ProgressPercent is < 0 or > 100)
            return Errors.Reports.InvalidProgressPercent;

        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
            return Errors.Reports.NotTeamLeader;

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == leader.TeamId);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.InProgress)
            return Errors.Reports.AssignmentNotInProgress;

        // Upload images if provided
        var uploadedUrls = new List<string>();
        foreach (var img in request.Images)
        {
            var folder = $"reports/{request.ReportId}/progress/{leader.TeamId}";
            using var stream = new MemoryStream(img.Bytes);
            var uploaded = await fileStorage.UploadAsync(stream, img.FileName, img.ContentType, folder, ct)
                .ConfigureAwait(false);
            uploadedUrls.Add(uploaded.Url);
        }

        assignment.UpdateProgress(request.ProgressPercent, request.ProgressNote, currentUser.UserId);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return new UpdateProgressResponse(uploadedUrls);
    }
}

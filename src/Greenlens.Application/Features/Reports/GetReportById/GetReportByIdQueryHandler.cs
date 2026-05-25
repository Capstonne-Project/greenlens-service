using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.GetReportById;

public sealed class GetReportByIdQueryHandler(
    IReportRepository reports)
    : IRequestHandler<GetReportByIdQuery, Result<ReportDetailResponse>>
{
    public async Task<Result<ReportDetailResponse>> Handle(
        GetReportByIdQuery request, CancellationToken ct)
    {
        var r = await reports.QueryAsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Media)
            .Include(x => x.Assignments).ThenInclude(a => a.Team)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (r is null)
            return Errors.Reports.ReportNotFound;

        var media = r.Media.Select(m => new ReportMediaItem(
            m.Id, m.Type.ToString(), m.Url, m.MimeType, m.SizeBytes)).ToList();

        var assignments = r.Assignments.Select(a => new ReportAssignmentItem(
            a.Id, a.TeamId, a.Team?.Name, a.Team?.TeamType.ToString() ?? "",
            a.Status.ToString(), a.Note, a.AssignedAt,
            a.StartedAt, a.CompletedAt)).ToList();

        return new ReportDetailResponse(
            r.Id, r.Code, r.ReporterId, r.IsAnonymous,
            r.CategoryId, r.Category.Code, r.Category.NameVi,
            r.Severity, r.SeveritySetBy, r.Status, r.Description,
            r.Latitude, r.Longitude, r.Address,
            r.WardCode, r.ProvinceCode,
            r.PriorityScore, r.ReporterCount, r.ReopenedCount,
            r.AiClassifiedType, r.AiConfidence,
            r.AssignedOfficerId, r.AssignedByOfficerId, r.AssignedOfficeId,
            media, assignments,
            r.CreatedAt, r.VerifiedAt, r.StartedAt,
            r.ResolvedAt, r.ClosedAt,
            r.SlaVerifyDueAt, r.SlaResolveDueAt);
    }
}

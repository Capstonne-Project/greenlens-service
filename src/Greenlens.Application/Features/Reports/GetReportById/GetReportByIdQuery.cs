using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReportById;

public sealed record GetReportByIdQuery(Guid Id) : IRequest<Result<ReportDetailResponse>>;

public sealed record ReportDetailResponse(
    Guid Id, string Code, Guid? ReporterId, bool IsAnonymous,
    Guid CategoryId, string CategoryCode, string CategoryName,
    Severity Severity, SeveritySource SeveritySetBy,
    ReportStatus Status, string? Description,
    decimal Latitude, decimal Longitude, string? Address,
    string? WardCode, string? ProvinceCode,
    decimal PriorityScore, int ReporterCount, int ReopenedCount,
    string? AiClassifiedType, decimal? AiConfidence,
    Guid? AssignedOfficerId, Guid? AssignedOfficeId,
    IReadOnlyList<ReportMediaItem> Media,
    IReadOnlyList<ReportAssignmentItem> Assignments,
    DateTime CreatedAt, DateTime? VerifiedAt, DateTime? StartedAt,
    DateTime? ResolvedAt, DateTime? ClosedAt,
    DateTime? SlaVerifyDueAt, DateTime? SlaResolveDueAt);

public sealed record ReportMediaItem(
    Guid Id, string MediaType, string Url, string MimeType, long SizeBytes);

public sealed record ReportAssignmentItem(
    Guid Id, Guid TeamId, string? TeamName, string TeamType,
    string Status, string? Note, DateTime AssignedAt,
    DateTime? StartedAt, DateTime? CompletedAt);

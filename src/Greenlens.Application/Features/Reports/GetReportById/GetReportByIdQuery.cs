using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReportById;

public sealed record GetReportByIdQuery(Guid Id) : IRequest<Result<ReportDetailResponse>>;

public sealed record ReportDetailResponse(
    Guid Id, string Code, Guid? ReporterId,
    /// <summary>Tên người gửi. Null khi báo cáo ẩn danh hoặc reporter đã xóa tài khoản (BR-REP-012, BR-AUTH-022).</summary>
    string? ReporterName,
    /// <summary>Avatar người gửi. Null khi ẩn danh, chưa đặt avatar, hoặc đã xóa tài khoản.</summary>
    string? ReporterAvatarUrl,
    Guid CategoryId, string CategoryCode, string CategoryName,
    Severity Severity, SeveritySource SeveritySetBy,
    ReportStatus Status, string? Description,
    decimal Latitude, decimal Longitude, string? Address,
    string? WardCode, string? ProvinceCode,
    decimal PriorityScore, int ReporterCount, int ReopenedCount,
    string? AiClassifiedType, decimal? AiConfidence,
    Guid? VerifiedBy, Guid? AssignedByOfficerId, Guid? AssignedOfficeId,
    IReadOnlyList<ReportMediaItem> Media,
    IReadOnlyList<ReportAssignmentItem> Assignments,
    IReadOnlyList<ReportWasteTagItem> WasteTags,
    string? AiSuggestedWasteTagCodes,
    DateTime CreatedAt, DateTime? VerifiedAt, DateTime? StartedAt,
    DateTime? ResolvedAt, DateTime? ClosedAt,
    DateTime? SlaVerifyDueAt, DateTime? SlaResolveDueAt,
    ReportSatisfactionInfo? Satisfaction,
    bool HasCurrentUserRated,
    bool HasPendingReopenRequest,
    PendingReopenRequestInfo? PendingReopenRequest,
    /// <summary>When this report is Duplicate — primary it was merged into (BR-REP-032).</summary>
    Guid? MergedIntoPrimaryReportId = null,
    string? MergedIntoPrimaryReportCode = null,
    /// <summary>
    /// Reports merged into this primary. imageUrl is projected from primary media
    /// where SourceReportId = child id (BR-REP-032).
    /// </summary>
    IReadOnlyList<MergedReportItem>? MergedReports = null,
    /// <summary>BR-REP-034: suspected violator recurrence near a recently Closed report.</summary>
    bool IsSuspectedViolationRecurrence = false,
    Guid? SuspectedRecurrenceOfReportId = null,
    PriorClosedReportSummary? PriorClosedReport = null);

public sealed record PendingReopenRequestInfo(
    Guid RequestId,
    string Reason,
    DateTime RequestedAt,
    IReadOnlyList<ReportMediaItem> EvidenceMedia);

/// <summary>Child report that was confirmed as duplicate of the primary (BR-REP-032).</summary>
public sealed record MergedReportItem(
    Guid Id,
    string Code,
    string? ImageUrl,
    DateTime CreatedAt,
    ReportStatus Status);

public sealed record ReportMediaItem(
    Guid Id, string MediaType, string Url, string MimeType, long SizeBytes);

public sealed record ReportAssignmentItem(
    Guid Id, Guid TeamId, string? TeamName, string TeamType,
    string Status, string? Note, DateTime AssignedAt,
    DateTime? StartedAt, DateTime? CompletedAt,
    int ProgressPercent, string? ProgressNote, DateTime? ProgressUpdatedAt);

public sealed record ReportWasteTagItem(
    Guid TagId, string Code, string NameVi, string NameEn, string? IconUrl);

/// <summary>Satisfaction feedback left by the reporter (BR-REP-018).</summary>
public sealed record ReportSatisfactionInfo(
    bool IsSatisfied, int? Rating, string? Comment, DateTime RatedAt);

/// <summary>Summary of the prior Closed report linked by BR-REP-034 recurrence flag.</summary>
public sealed record PriorClosedReportSummary(
    Guid Id,
    string Code,
    DateTime? ClosedAt,
    string CategoryCode,
    bool HadPriorInspection);

using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Greenlens.Application.Common.Models;

namespace Greenlens.Application.Features.Inspection.GetInspectionQueue;

/// <summary>BR-INS-002: Inspector views inspection tasks assigned to their team.</summary>
public sealed record GetInspectionQueueQuery(
    int Page = 1,
    int PageSize = 20,
    InspectionStatus? Status = null) : IRequest<Result<GetInspectionQueueResponse>>;

public sealed record GetInspectionQueueResponse(
    List<InspectionQueueItemDto> Items,
    PaginationMeta Pagination);

public sealed record InspectionQueueItemDto(
    Guid Id,
    Guid ReportId,
    string ReportCode,
    InspectionStatus Status,
    string? Address,
    string? WardCode,
    string? ViolatorName,
    string? ViolationDescription,
    ViolationLevel? ViolationLevel,
    decimal? PenaltyAmount,
    bool IsRepeatOffender,
    DateTime? SlaInspectionDueAt,
    DateTime CreatedAt,
    /// <summary>Toạ độ hiện trường (từ Report) — cho Inspector map view.</summary>
    decimal Latitude,
    decimal Longitude);

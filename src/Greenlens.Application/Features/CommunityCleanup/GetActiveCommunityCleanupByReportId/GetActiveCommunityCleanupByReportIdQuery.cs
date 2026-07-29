using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetActiveCommunityCleanupByReportId;

/// <summary>
/// BR-CMU-003: at most one active (not Completed/Cancelled) community cleanup event per report.
/// Returns success with null data when the report has no active event — this is a normal state
/// (most reports never get a community program), not an error.
/// </summary>
public sealed record GetActiveCommunityCleanupByReportIdQuery(Guid ReportId)
    : IRequest<Result<CommunityCleanupEventDetailResponse?>>;

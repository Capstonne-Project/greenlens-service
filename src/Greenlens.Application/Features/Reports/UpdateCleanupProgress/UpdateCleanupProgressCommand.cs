using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.UpdateCleanupProgress;

/// <summary>
/// BR-CLN-004: Team leader updates cleanup progress (must be ≥ 1/day).
/// </summary>
public sealed record UpdateCleanupProgressCommand(
    Guid ReportId,
    Guid TeamId,
    int Percent,
    string? Note = null) : IRequest<Result>;

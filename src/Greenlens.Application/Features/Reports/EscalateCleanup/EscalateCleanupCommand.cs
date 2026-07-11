using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.EscalateCleanup;

/// <summary>
/// BR-CLN-006: Team escalates task to LEO — beyond their capability.
/// Reason must be ≥ 20 characters.
/// </summary>
public sealed record EscalateCleanupCommand(
    Guid ReportId,
    Guid TeamId,
    string Reason) : IRequest<Result>;

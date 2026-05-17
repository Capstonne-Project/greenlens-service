using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.IssuePenalty;

/// <summary>
/// Inspection Team Leader issues penalty. BR-INS-012.
/// Report only transitions to PenaltyIssued when ALL assignments are completed.
/// </summary>
public sealed record IssuePenaltyCommand(
    Guid ReportId,
    Guid TeamId) : IRequest<Result>;

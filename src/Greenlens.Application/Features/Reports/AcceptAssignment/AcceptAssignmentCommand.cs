using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.AcceptAssignment;

/// <summary>Team accepts the assignment. ASSIGNED → IN_PROGRESS (report) + Assigned → InProgress (assignment).</summary>
public sealed record AcceptAssignmentCommand(
    Guid ReportId,
    Guid TeamId) : IRequest<Result>;

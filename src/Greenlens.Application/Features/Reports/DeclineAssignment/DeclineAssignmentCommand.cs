using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.DeclineAssignment;

/// <summary>Team declines task within 2h window. BR-CLN-007, BR-INS-003.</summary>
public sealed record DeclineAssignmentCommand(
    Guid ReportId,
    Guid TeamId,
    string Reason) : IRequest<Result>;

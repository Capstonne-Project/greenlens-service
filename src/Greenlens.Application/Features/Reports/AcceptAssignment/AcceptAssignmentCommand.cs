using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.AcceptAssignment;

/// <summary>Team leader accepts an assigned task. TeamId resolved from JWT token.</summary>
public sealed record AcceptAssignmentCommand(Guid ReportId) : IRequest<Result>;

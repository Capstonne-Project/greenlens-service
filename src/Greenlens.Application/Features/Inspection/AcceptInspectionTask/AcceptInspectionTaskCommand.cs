using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.AcceptInspectionTask;

/// <summary>BR-INS-033: Inspection team accepts assigned task. Draft → InProgress.</summary>
public sealed record AcceptInspectionTaskCommand(Guid InspectionId) : IRequest<Result>;

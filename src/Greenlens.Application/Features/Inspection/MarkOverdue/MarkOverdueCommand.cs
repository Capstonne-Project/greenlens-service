using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.MarkOverdue;

/// <summary>BR-INS-021: System marks inspection as overdue when payment deadline passes.</summary>
public sealed record MarkOverdueCommand(Guid InspectionId) : IRequest<Result>;

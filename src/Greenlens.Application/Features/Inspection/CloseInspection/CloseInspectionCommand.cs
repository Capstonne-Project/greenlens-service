using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.CloseInspection;

/// <summary>Close inspection after full payment. Paid → Closed.</summary>
public sealed record CloseInspectionCommand(
    Guid InspectionId,
    string? Reason = null) : IRequest<Result>;

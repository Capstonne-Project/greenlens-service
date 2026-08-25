using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionProgress;

/// <summary>
/// BR-INS-031: Update inspection progress. Must be ≥ 1/day when InProgress.
/// </summary>
public sealed record UpdateInspectionProgressCommand(
    Guid InspectionId,
    int Percent,
    decimal Latitude,
    decimal Longitude,
    string? Note = null) : IRequest<Result>;

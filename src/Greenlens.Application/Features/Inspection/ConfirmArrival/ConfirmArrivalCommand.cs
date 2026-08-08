using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.ConfirmArrival;

/// <summary>BR-INS-033: Soft GPS arrival confirmation at investigation site.</summary>
public sealed record ConfirmArrivalCommand(
    Guid InspectionId,
    decimal Latitude,
    decimal Longitude,
    string? Note = null) : IRequest<Result>;

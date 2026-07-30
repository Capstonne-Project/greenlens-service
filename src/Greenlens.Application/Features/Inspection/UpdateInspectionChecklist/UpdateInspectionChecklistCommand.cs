using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionChecklist;

/// <summary>BR-INS-033: Update text fields on the hardcoded inspection checklist.</summary>
public sealed record UpdateInspectionChecklistCommand(
    Guid InspectionId,
    string ViolationStatusText,
    string? OtherDescription = null) : IRequest<Result>;

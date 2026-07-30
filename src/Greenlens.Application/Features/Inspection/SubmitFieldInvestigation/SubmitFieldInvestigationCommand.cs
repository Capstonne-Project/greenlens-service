using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.SubmitFieldInvestigation;

/// <summary>BR-INS-033: Team Leader submits completed field investigation checklist.</summary>
public sealed record SubmitFieldInvestigationCommand(Guid InspectionId) : IRequest<Result>;

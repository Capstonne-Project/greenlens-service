using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.UpdateProgress;

/// <summary>Team leader updates progress mid-task (%, note). Status stays InProgress.</summary>
public sealed record UpdateProgressCommand(
    Guid ReportId,
    Guid TeamId,
    int ProgressPercent,
    string? ProgressNote) : IRequest<Result>;

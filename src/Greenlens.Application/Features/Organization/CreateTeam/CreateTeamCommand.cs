using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateTeam;

/// <summary>
/// LEO tạo team cộng đồng (Cleanup hoặc Inspection).
/// LocalOfficeId tự resolve từ token — không cần truyền.
/// </summary>
/// <remarks>Implements: BR-ORG-003, BR-CLN-005.</remarks>
public sealed record CreateTeamCommand(
    string Name,
    TeamType TeamType,
    List<Guid>? WasteTagIds) : IRequest<Result<CreateTeamResponse>>;

public sealed record CreateTeamResponse(
    Guid Id,
    string Name,
    Guid LocalOfficeId,
    string TeamType,
    IReadOnlyList<WasteTagSummaryDto> WasteTags);

using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization.Common;

/// <summary>Validate and sync WasteTags on Cleanup teams.</summary>
/// <remarks>Implements: BR-CLN-005.</remarks>
public sealed class TeamWasteTagService(
    IWasteTagRepository wasteTags,
    ITeamWasteTagRepository teamWasteTags)
{
    public Result ValidateForTeamType(TeamType teamType, IReadOnlyList<Guid>? wasteTagIds)
    {
        if (teamType == TeamType.Inspection && wasteTagIds is { Count: > 0 })
            return Errors.Organization.WasteTagsNotAllowedForInspectionTeam;

        if (teamType == TeamType.Cleanup && (wasteTagIds is null || wasteTagIds.Count == 0))
            return Errors.Organization.CleanupTeamRequiresWasteTags;

        return Result.Success();
    }

    public async Task<Result> ValidateActiveTagIdsAsync(
        IReadOnlyList<Guid> wasteTagIds,
        CancellationToken ct)
    {
        if (wasteTagIds.Count == 0)
            return Result.Success();

        var tags = await wasteTags.GetByIdsAsync(wasteTagIds.ToList(), ct).ConfigureAwait(false);
        if (tags.Count != wasteTagIds.Count)
            return Errors.Reports.WasteTagNotFound;

        if (tags.Any(t => !t.IsActive))
            return Errors.Reports.WasteTagInactive;

        return Result.Success();
    }

    public async Task<Result> ReplaceTeamTagsAsync(
        EnvironmentalTeam team,
        IReadOnlyList<Guid> wasteTagIds,
        CancellationToken ct)
    {
        var typeCheck = ValidateForTeamType(team.TeamType, wasteTagIds);
        if (!typeCheck.IsSuccess)
            return typeCheck;

        var tagCheck = await ValidateActiveTagIdsAsync(wasteTagIds, ct).ConfigureAwait(false);
        if (!tagCheck.IsSuccess)
            return tagCheck;

        var existing = await teamWasteTags.GetByTeamIdAsync(team.Id, ct).ConfigureAwait(false);
        if (existing.Count > 0)
            teamWasteTags.RemoveRange(existing);

        var newTags = wasteTagIds
            .Distinct()
            .Select(id => TeamWasteTag.Create(team.Id, id))
            .ToList();

        if (newTags.Count > 0)
            teamWasteTags.AddRange(newTags);

        return Result.Success();
    }

    public static IReadOnlyList<WasteTagSummaryDto> MapTags(EnvironmentalTeam team) =>
        team.TeamType == TeamType.Inspection
            ? []
            : team.WasteTags
                .Where(tw => tw.WasteTag is not null)
                .OrderBy(tw => tw.WasteTag!.DisplayOrder)
                .Select(tw => new WasteTagSummaryDto(
                    tw.WasteTagId,
                    tw.WasteTag!.Code,
                    tw.WasteTag.NameVi,
                    tw.WasteTag.NameEn,
                    tw.WasteTag.IconUrl))
                .ToList();

    public static int CountMatchingTags(EnvironmentalTeam team, IReadOnlyCollection<Guid> prioritizeIds)
    {
        if (prioritizeIds.Count == 0 || team.TeamType == TeamType.Inspection)
            return 0;

        var teamTagIds = team.WasteTags.Select(tw => tw.WasteTagId).ToHashSet();
        return prioritizeIds.Count(id => teamTagIds.Contains(id));
    }
}

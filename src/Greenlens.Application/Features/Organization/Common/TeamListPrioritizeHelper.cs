using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization.Common;

/// <summary>Resolves waste-tag IDs used for team list prioritization.</summary>
public static class TeamListPrioritizeHelper
{
    public static async Task<Result<IReadOnlyList<Guid>>> ResolvePrioritizeTagIdsForLeoAsync(
        Guid? reportId,
        IReadOnlyList<Guid>? wasteTagIds,
        IReportRepository reports,
        IReportWasteTagRepository reportWasteTags,
        Guid? leoLocalOfficeId,
        CancellationToken ct)
    {
        if (reportId.HasValue)
        {
            var report = await reports.GetByIdAsync(reportId.Value, ct).ConfigureAwait(false);
            if (report is null)
                return Errors.Reports.ReportNotFound;

            if (leoLocalOfficeId.HasValue &&
                report.AssignedOfficeId != leoLocalOfficeId)
                return Errors.Reports.OutsideJurisdiction;

            var reportTags = await reportWasteTags.GetByReportIdAsync(reportId.Value, ct)
                .ConfigureAwait(false);
            return reportTags.Select(t => t.WasteTagId).Distinct().ToList();
        }

        if (wasteTagIds is { Count: > 0 })
            return wasteTagIds.Distinct().ToList();

        return Array.Empty<Guid>();
    }

    public static async Task<Result<IReadOnlyList<Guid>>> ResolvePrioritizeTagIdsForCompanyAsync(
        Guid? reportId,
        IReadOnlyList<Guid>? wasteTagIds,
        IReportRepository reports,
        IReportWasteTagRepository reportWasteTags,
        Guid companyId,
        CancellationToken ct)
    {
        if (reportId.HasValue)
        {
            var report = await reports.GetByIdAsync(reportId.Value, ct).ConfigureAwait(false);
            if (report is null)
                return Errors.Reports.ReportNotFound;

            if (report.AssignedCompanyId != companyId)
                return Errors.Organization.CrossCompanyAccess;

            var reportTags = await reportWasteTags.GetByReportIdAsync(reportId.Value, ct)
                .ConfigureAwait(false);
            return reportTags.Select(t => t.WasteTagId).Distinct().ToList();
        }

        if (wasteTagIds is { Count: > 0 })
            return wasteTagIds.Distinct().ToList();

        return Array.Empty<Guid>();
    }
}

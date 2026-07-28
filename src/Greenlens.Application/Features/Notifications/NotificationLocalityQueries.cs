using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Notifications;

internal sealed record NotificationLocality(string WardName, string ProvinceName)
{
    internal static NotificationLocality Empty { get; } = new(string.Empty, string.Empty);
}

internal static class NotificationLocalityQueries
{
    internal static async Task<NotificationLocality> FromReportIdAsync(
        IApplicationDbContext db,
        Guid reportId,
        CancellationToken ct)
    {
        var wardCode = await db.Set<Report>()
            .AsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => r.WardCode)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return await FromWardCodeAsync(db, wardCode, ct).ConfigureAwait(false);
    }

    internal static async Task<NotificationLocality> FromOfficeIdAsync(
        IApplicationDbContext db,
        Guid officeId,
        CancellationToken ct)
    {
        var wardCode = await db.Set<LocalOffice>()
            .AsNoTracking()
            .Where(o => o.Id == officeId)
            .Select(o => o.WardCode)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return await FromWardCodeAsync(db, wardCode, ct).ConfigureAwait(false);
    }

    internal static async Task<NotificationLocality> FromWardCodeAsync(
        IApplicationDbContext db,
        string? wardCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(wardCode))
            return NotificationLocality.Empty;

        var ward = await db.Set<Ward>()
            .AsNoTracking()
            .Where(w => w.Code == wardCode)
            .Select(w => new { w.Name, w.ProvinceCode })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (ward is null)
            return NotificationLocality.Empty;

        var provinceName = await db.Set<Province>()
            .AsNoTracking()
            .Where(p => p.Code == ward.ProvinceCode)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return new NotificationLocality(ward.Name, provinceName ?? string.Empty);
    }

    internal static Dictionary<string, string> ApplyLocality(
        Dictionary<string, string> placeholders,
        NotificationLocality locality)
    {
        var wardName = NotificationVietnameseLabels.DisplayWardName(locality.WardName);
        var provinceName = NotificationVietnameseLabels.DisplayProvinceName(locality.ProvinceName);

        placeholders["ward_name"] = wardName;
        placeholders["province_name"] = provinceName;
        // Legacy alias used by staff-invitation templates.
        placeholders["office_name"] = wardName;
        return placeholders;
    }

    internal static async Task<Dictionary<string, string>> EnrichFromReportIdAsync(
        IApplicationDbContext db,
        Dictionary<string, string> placeholders,
        Guid reportId,
        CancellationToken ct)
    {
        var locality = await FromReportIdAsync(db, reportId, ct).ConfigureAwait(false);
        return ApplyLocality(placeholders, locality);
    }

    internal static async Task<Dictionary<string, string>> EnrichFromWardCodeAsync(
        IApplicationDbContext db,
        Dictionary<string, string> placeholders,
        string? wardCode,
        CancellationToken ct)
    {
        var locality = await FromWardCodeAsync(db, wardCode, ct).ConfigureAwait(false);
        return ApplyLocality(placeholders, locality);
    }

    internal static async Task<Dictionary<string, string>> EnrichFromOfficeIdAsync(
        IApplicationDbContext db,
        Dictionary<string, string> placeholders,
        Guid officeId,
        CancellationToken ct)
    {
        var locality = await FromOfficeIdAsync(db, officeId, ct).ConfigureAwait(false);
        return ApplyLocality(placeholders, locality);
    }
}

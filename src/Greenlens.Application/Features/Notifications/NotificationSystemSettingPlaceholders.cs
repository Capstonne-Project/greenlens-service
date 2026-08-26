using System.Globalization;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.Features.Notifications;

/// <summary>
/// Config-driven placeholder values for notification templates (Case B).
/// Merged automatically before template render so admin-edited bodies stay in sync with system_settings.
/// </summary>
internal static class NotificationSystemSettingPlaceholders
{
    internal static Dictionary<string, string> Build(ISystemSettingsProvider settings)
    {
        var (staleHours, escalateHours, _) = ModuleSystemSettings.CleanupProgress(settings);
        var (nearbyMeters, _, maxPerTypePerDay) = ModuleSystemSettings.Notifications(settings);
        var slaVerifyHours = ModuleSystemSettings.ReportSla(settings).VerifyHours;

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sla_verify_hours"] = slaVerifyHours.ToString(CultureInfo.InvariantCulture),
            ["overdue_pending_hours"] = ModuleSystemSettings.SlaOverduePendingHours(settings)
                .ToString(CultureInfo.InvariantCulture),
            ["unassigned_verified_hours"] = ModuleSystemSettings.SlaUnassignedVerifiedHours(settings)
                .ToString(CultureInfo.InvariantCulture),
            ["auto_close_resolved_days"] = ReportSystemSettings.AutoCloseResolvedDays(settings)
                .ToString(CultureInfo.InvariantCulture),
            ["progress_stale_hours"] = staleHours.ToString(CultureInfo.InvariantCulture),
            ["progress_escalate_hours"] = escalateHours.ToString(CultureInfo.InvariantCulture),
            ["nearby_report_radius_meters"] = nearbyMeters.ToString(CultureInfo.InvariantCulture),
            ["nearby_radius_km"] = FormatNearbyRadiusKm(nearbyMeters),
            ["invitation_response_days"] = ModuleSystemSettings.InvitationResponseDays(settings)
                .ToString(CultureInfo.InvariantCulture),
            ["staff_invitation_expiry_days"] = ModuleSystemSettings.StaffInvitationExpiryDays(settings)
                .ToString(CultureInfo.InvariantCulture),
            ["check_in_reminder_minutes"] = ModuleSystemSettings.CommunityCheckInReminderMinutes(settings)
                .ToString(CultureInfo.InvariantCulture),
            ["duplicate_radius_meters"] = ReportSystemSettings.DuplicateRadiusMeters(settings)
                .ToString(CultureInfo.InvariantCulture),
            ["max_notifications_per_type_per_day"] = maxPerTypePerDay.ToString(CultureInfo.InvariantCulture),
        };
    }

    internal static Dictionary<string, string> Merge(
        IReadOnlyDictionary<string, string>? caller,
        ISystemSettingsProvider settings)
    {
        var merged = Build(settings);
        if (caller is null)
            return merged;

        foreach (var (key, value) in caller)
            merged[key] = value;

        return merged;
    }

    private static string FormatNearbyRadiusKm(int meters)
    {
        var km = meters / 1000m;
        return km % 1m == 0m
            ? ((int)km).ToString(CultureInfo.InvariantCulture)
            : km.ToString("0.#", CultureInfo.InvariantCulture);
    }
}

using System.Text.Json;
using Greenlens.Application.BusinessRules;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Typed accessors for non-report system settings with code fallbacks.</summary>
public static class ModuleSystemSettings
{
    public static ReportSlaPolicy ReportSla(ISystemSettingsProvider s) =>
        new(
            s.GetInt(SystemSettingModule.Sla, SystemSettingKeys.Sla.VerifyHours, 24),
            s.GetInt(SystemSettingModule.Sla, SystemSettingKeys.Sla.ResolveDaysCritical, 3),
            s.GetInt(SystemSettingModule.Sla, SystemSettingKeys.Sla.ResolveDaysHigh, 5),
            s.GetInt(SystemSettingModule.Sla, SystemSettingKeys.Sla.ResolveDaysMedium, 7),
            s.GetInt(SystemSettingModule.Sla, SystemSettingKeys.Sla.ResolveDaysLow, 10));

    public static InspectionSlaPolicy InspectionSla(ISystemSettingsProvider s) =>
        new(
            s.GetInt(SystemSettingModule.Inspection, SystemSettingKeys.Inspection.ResolveDaysCritical, 3),
            s.GetInt(SystemSettingModule.Inspection, SystemSettingKeys.Inspection.ResolveDaysHigh, 5),
            s.GetInt(SystemSettingModule.Inspection, SystemSettingKeys.Inspection.ResolveDaysMedium, 7),
            s.GetInt(SystemSettingModule.Inspection, SystemSettingKeys.Inspection.ResolveDaysLow, 10));

    public static int SlaOverduePendingHours(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Sla, SystemSettingKeys.Sla.OverduePendingHours, 72);

    public static int SlaUnassignedVerifiedHours(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Sla, SystemSettingKeys.Sla.UnassignedVerifiedHours, 24);

    public static (int SeverityWeight, int ReporterWeight, int AgeDivisorHours, int SlaBreachBoost) OfficerPriority(
        ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Officer, SystemSettingKeys.Officer.PrioritySeverityWeight, 3),
            s.GetInt(SystemSettingModule.Officer, SystemSettingKeys.Officer.PriorityReporterCountWeight, 2),
            s.GetInt(SystemSettingModule.Officer, SystemSettingKeys.Officer.PriorityAgeDivisorHours, 24),
            s.GetInt(SystemSettingModule.Officer, SystemSettingKeys.Officer.PrioritySlaVerifyBreachBoost, 100));

    public static (int StaleHours, int EscalateHours, int DeclineWindowHours) CleanupProgress(
        ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Cleanup, SystemSettingKeys.Cleanup.ProgressStaleHours, 24),
            s.GetInt(SystemSettingModule.Cleanup, SystemSettingKeys.Cleanup.ProgressEscalateHours, 48),
            s.GetInt(SystemSettingModule.Cleanup, SystemSettingKeys.Cleanup.DeclineWindowHours, 24));

    public static (int MaxPerHour, int MaxPerDay, int LockSeconds) SubmitRateLimits(ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.RateLimits, SystemSettingKeys.RateLimits.SubmitMaxPerHour, 5),
            s.GetInt(SystemSettingModule.RateLimits, SystemSettingKeys.RateLimits.SubmitMaxPerDay, 20),
            s.GetInt(SystemSettingModule.RateLimits, SystemSettingKeys.RateLimits.SubmitLockSeconds, 3600));

    public static (decimal MinLat, decimal MaxLat, decimal MinLng, decimal MaxLng) VietnamBounds(
        ISystemSettingsProvider s) =>
        (
            s.GetDecimal(SystemSettingModule.Geo, SystemSettingKeys.Geo.VietnamMinLatitude, 8m),
            s.GetDecimal(SystemSettingModule.Geo, SystemSettingKeys.Geo.VietnamMaxLatitude, 24m),
            s.GetDecimal(SystemSettingModule.Geo, SystemSettingKeys.Geo.VietnamMinLongitude, 102m),
            s.GetDecimal(SystemSettingModule.Geo, SystemSettingKeys.Geo.VietnamMaxLongitude, 110m));

    public static int CheckInMaxDistanceMeters(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Geo, SystemSettingKeys.Geo.CheckInMaxDistanceMeters, 200);

    public static int ExifGpsMismatchMeters(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Geo, SystemSettingKeys.Geo.ExifGpsMismatchMeters, 200);

    public static int InspectionSoftGpsMeters(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Geo, SystemSettingKeys.Geo.InspectionSoftGpsMeters, 200);

    public static int ProgressUpdateMaxDistanceMeters(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Geo, SystemSettingKeys.Geo.ProgressUpdateMaxDistanceMeters, 200);

    public static int MapCoordinateDecimalPlaces(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Map, SystemSettingKeys.Map.PublicCoordinateDecimalPlaces, 4);

    public static (decimal MaxLatSpan, decimal MaxLngSpan) MapBoundingSpans(ISystemSettingsProvider s) =>
        (
            s.GetDecimal(SystemSettingModule.Map, SystemSettingKeys.Map.MaxBoundingLatSpan, 6m),
            s.GetDecimal(SystemSettingModule.Map, SystemSettingKeys.Map.MaxBoundingLngSpan, 8m));

    public static (int DefaultDetailLimit, int MaxDetailLimit) MapDetailLimits(ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Map, SystemSettingKeys.Map.DefaultDetailLimit, 200),
            s.GetInt(SystemSettingModule.Map, SystemSettingKeys.Map.MaxDetailLimit, 500));

    public static int MapDefaultGridLevel(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Map, SystemSettingKeys.Map.DefaultGridLevel, 3);

    public static int MapMaxAggregateRows(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Map, SystemSettingKeys.Map.MaxAggregateRows, 50_000);

    public static (int MinDays, int MaxDays) MapViewportDayBounds(ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Map, SystemSettingKeys.Map.ViewportMinDays, 7),
            s.GetInt(SystemSettingModule.Map, SystemSettingKeys.Map.ViewportMaxDays, 90));

    public static (int NearbyRadiusMeters, int MaxRecipients, int MaxPerTypePerDay) Notifications(
        ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Notifications, SystemSettingKeys.Notifications.NearbyReportRadiusMeters, 2000),
            s.GetInt(SystemSettingModule.Notifications, SystemSettingKeys.Notifications.NearbyReportMaxRecipients, 100),
            s.GetInt(SystemSettingModule.Notifications, SystemSettingKeys.Notifications.MaxPerTypePerDay, 20));

    public static (int MaxFailedAttempts, int LockoutMinutes) AuthLockout(
        ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Auth, SystemSettingKeys.Auth.MaxFailedLoginAttempts, 5),
            s.GetInt(SystemSettingModule.Auth, SystemSettingKeys.Auth.LockoutMinutes, 30));

    public static int CommentEditWindowMinutes(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Comments, SystemSettingKeys.Comments.EditWindowMinutes, 15);

    public static int CommentBanDurationDays(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Comments, SystemSettingKeys.Comments.BanDurationDays, 7);

    public static int StaffInvitationExpiryDays(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Organization, SystemSettingKeys.Organization.StaffInvitationExpiryDays, 7);

    public static int InvitationResponseDays(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Organization, SystemSettingKeys.Organization.InvitationResponseDays, 7);

    public static int[] ContractWarningDays(ISystemSettingsProvider s)
    {
        var json = s.GetString(
            SystemSettingModule.Organization,
            SystemSettingKeys.Organization.ContractWarningDays,
            "[30,7,1]");
        return ParsePositiveIntJsonArray(json, [30, 7, 1]);
    }

    /// <summary>Largest contract warning window — used for DEO dashboard expiry alerts.</summary>
    public static int ContractAlertHorizonDays(ISystemSettingsProvider s)
    {
        var days = ContractWarningDays(s);
        return days.Length == 0 ? 30 : days.Max();
    }

    public static int CommunityBeforeImagesMax(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.CommunityCleanup, SystemSettingKeys.CommunityCleanup.BeforeImagesMax, 5);

    public static int CommunityCheckInReminderMinutes(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.CommunityCleanup, SystemSettingKeys.CommunityCleanup.CheckInReminderMinutesBeforeStart, 15);

    public static (int MediaYears, int AuditMonths, int StatusHistoryMonths) DataRetention(ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.DataRetention, SystemSettingKeys.DataRetention.MediaRetentionYears, 2),
            s.GetInt(SystemSettingModule.DataRetention, SystemSettingKeys.DataRetention.AuditLogRetentionMonths, 12),
            s.GetInt(SystemSettingModule.DataRetention, SystemSettingKeys.DataRetention.StatusHistoryRetentionMonths, 12));

    public static int AccountSoftDeleteRetentionDays(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Auth, SystemSettingKeys.Auth.AccountSoftDeleteRetentionDays, 90);

    public static int OtpMaxAttempts(ISystemSettingsProvider s) =>
        s.GetInt(SystemSettingModule.Auth, SystemSettingKeys.Auth.OtpMaxAttempts, 5);

    public static (int TimeoutSeconds, int CompareTimeoutSeconds, int TempImageTtlSeconds, int PresignUploadTtlMinutes) Ai(
        ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Ai, SystemSettingKeys.Ai.TimeoutSeconds, 5),
            s.GetInt(SystemSettingModule.Ai, SystemSettingKeys.Ai.CompareTimeoutSeconds, 15),
            s.GetInt(SystemSettingModule.Ai, SystemSettingKeys.Ai.TempImageTtlSeconds, 900),
            s.GetInt(SystemSettingModule.Ai, SystemSettingKeys.Ai.PresignUploadTtlMinutes, 15));

    public static (int RejectMin, int ReopenMin) ValidationReasonLengths(
        ISystemSettingsProvider s) =>
        (
            s.GetInt(SystemSettingModule.Validation, SystemSettingKeys.Validation.RejectReasonMinLength, 20),
            s.GetInt(SystemSettingModule.Validation, SystemSettingKeys.Validation.ReopenReasonMinLength, 20));

    public static decimal ComputePriorityScore(
        ISystemSettingsProvider s,
        Severity severity,
        int reporterCount,
        decimal ageHours,
        bool slaVerifyBreached) =>
        ComputePriorityScore(OfficerPriority(s), severity, reporterCount, ageHours, slaVerifyBreached);

    public static decimal ComputePriorityScore(
        (int SeverityWeight, int ReporterWeight, int AgeDivisorHours, int SlaBreachBoost) weights,
        Severity severity,
        int reporterCount,
        decimal ageHours,
        bool slaVerifyBreached)
    {
        var score = (int)severity * weights.SeverityWeight
                    + reporterCount * weights.ReporterWeight
                    + ageHours / weights.AgeDivisorHours;

        if (slaVerifyBreached)
            score += weights.SlaBreachBoost;

        return Math.Round(score, 2);
    }

    private static int[] ParsePositiveIntJsonArray(string json, int[] fallback)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<int[]>(json);
            if (parsed is null || parsed.Length == 0)
                return fallback;

            var normalized = parsed
                .Where(day => day > 0)
                .Distinct()
                .OrderByDescending(day => day)
                .ToArray();

            return normalized.Length == 0 ? fallback : normalized;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}

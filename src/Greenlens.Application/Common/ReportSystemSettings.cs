using Greenlens.Application.BusinessRules;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Typed accessors for report module system settings with code fallbacks.</summary>
public static class ReportSystemSettings
{
    public static int DuplicateRadiusMeters(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.DuplicateRadiusMeters, 25);

    public static int DuplicateTimeWindowHours(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.DuplicateTimeWindowHours, 0);

    public static int DuplicateMaxCandidates(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.DuplicateMaxCandidates, 20);

    public static decimal DuplicateMergePointsRatio(ISystemSettingsProvider settings) =>
        settings.GetDecimal(SystemSettingModule.Reports, SystemSettingKeys.Reports.DuplicateMergePointsRatio, 0.5m);

    public static int RecurrenceRadiusMeters(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.RecurrenceRadiusMeters, 25);

    public static int RecurrenceMinDaysAfterClose(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.RecurrenceMinDaysAfterClose, 0);

    public static int RecurrenceMaxDaysAfterClose(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.RecurrenceMaxDaysAfterClose, 30);

    public static int MaxImagesPerReport(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.MaxImagesPerReport, 5);

    public static int MaxImageSizeMegabytes(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.MaxImageSizeMb, 10);

    public static long MaxImageSizeBytes(ISystemSettingsProvider settings) =>
        (long)MaxImageSizeMegabytes(settings) * 1024 * 1024;

    public static int AutoCloseResolvedDays(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.AutoCloseResolvedDays, 2);

    public static int ReopenWindowDays(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.ReopenWindowDays, 7);

    public static int MaxApprovedReopens(ISystemSettingsProvider settings) =>
        settings.GetInt(SystemSettingModule.Reports, SystemSettingKeys.Reports.MaxApprovedReopens, 1);
}

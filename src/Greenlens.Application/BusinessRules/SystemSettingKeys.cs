using Greenlens.Domain.Enums;

namespace Greenlens.Application.BusinessRules;

/// <summary>Well-known system setting keys grouped by module.</summary>
public static class SystemSettingKeys
{
    public static class Reports
    {
        public const string DuplicateRadiusMeters = "duplicate_radius_meters";
        public const string DuplicateTimeWindowHours = "duplicate_time_window_hours";
        public const string DuplicateMaxCandidates = "duplicate_max_candidates";
        public const string DuplicateMergePointsRatio = "duplicate_merge_points_ratio";
        public const string RecurrenceRadiusMeters = "recurrence_radius_meters";
        public const string RecurrenceLookbackDays = "recurrence_lookback_days";
        public const string RecurrenceMinDaysAfterClose = "recurrence_min_days_after_close";
        public const string RecurrenceMaxDaysAfterClose = "recurrence_max_days_after_close";
        public const string MaxImagesPerReport = "max_images_per_report";
        public const string MaxImageSizeBytes = "max_image_size_bytes";
        public const string MaxDraftsPerUser = "max_drafts_per_user";
        public const string DraftRetentionDays = "draft_retention_days";
        public const string AutoCloseResolvedDays = "auto_close_resolved_days";
        public const string ReopenWindowDays = "reopen_window_days";
        public const string MaxApprovedReopens = "max_approved_reopens";
        public const string FlagNotifyThreshold = "flag_notify_threshold";
    }

    public static class Sla
    {
        public const string VerifyHours = "sla_verify_hours";
        public const string ResolveDaysCritical = "sla_resolve_days_critical";
        public const string ResolveDaysHigh = "sla_resolve_days_high";
        public const string ResolveDaysMedium = "sla_resolve_days_medium";
        public const string ResolveDaysLow = "sla_resolve_days_low";
        public const string OverduePendingHours = "overdue_pending_hours";
        public const string UnassignedVerifiedHours = "unassigned_verified_hours";
        public const string VerifyBreachPriorityBoost = "sla_verify_breach_priority_boost";
    }

    public static class Geo
    {
        public const string VietnamMinLatitude = "vietnam_min_latitude";
        public const string VietnamMaxLatitude = "vietnam_max_latitude";
        public const string VietnamMinLongitude = "vietnam_min_longitude";
        public const string VietnamMaxLongitude = "vietnam_max_longitude";
        public const string CheckInMaxDistanceMeters = "check_in_max_distance_meters";
        public const string ExifGpsMismatchMeters = "exif_gps_mismatch_meters";
        public const string InspectionSoftGpsMeters = "inspection_soft_gps_meters";
    }

    public static class Map
    {
        public const string PublicCoordinateDecimalPlaces = "public_coordinate_decimal_places";
        public const string MaxBoundingLatSpan = "map_max_bounding_lat_span";
        public const string MaxBoundingLngSpan = "map_max_bounding_lng_span";
        public const string DefaultDetailLimit = "map_default_detail_limit";
        public const string MaxDetailLimit = "map_max_detail_limit";
        public const string DefaultGridLevel = "map_default_grid_level";
        public const string ViewportDefaultDays = "map_viewport_default_days";
        public const string ViewportMinDays = "map_viewport_min_days";
        public const string ViewportMaxDays = "map_viewport_max_days";
        public const string MaxAggregateRows = "map_max_aggregate_rows";
    }

    public static class Officer
    {
        public const string PrioritySeverityWeight = "priority_severity_weight";
        public const string PriorityReporterCountWeight = "priority_reporter_count_weight";
        public const string PriorityAgeDivisorHours = "priority_age_divisor_hours";
        public const string PrioritySlaVerifyBreachBoost = "priority_sla_verify_breach_boost";
    }

    public static class Cleanup
    {
        public const string ProgressStaleHours = "progress_stale_hours";
        public const string ProgressEscalateHours = "progress_escalate_hours";
        public const string DeclineWindowHours = "decline_window_hours";
        public const string ProgressUpdateIntervalHours = "progress_update_interval_hours";
    }

    public static class Notifications
    {
        public const string NearbyReportRadiusMeters = "nearby_report_radius_meters";
        public const string NearbyReportMaxRecipients = "nearby_report_max_recipients";
        public const string MaxPerTypePerDay = "max_notifications_per_type_per_day";
    }

    public static class Gamification
    {
        public const string DuplicateMergePointsRatio = "duplicate_merge_points_ratio";
    }

    public static class Auth
    {
        public const string MaxFailedLoginAttempts = "max_failed_login_attempts";
        public const string LockoutMinutes = "lockout_minutes";
        public const string CaptchaAfterFailedAttempts = "captcha_after_failed_attempts";
        public const string OtpMaxAttempts = "otp_max_attempts";
        public const string AccountSoftDeleteRetentionDays = "account_soft_delete_retention_days";
    }

    public static class Comments
    {
        public const string EditWindowMinutes = "comment_edit_window_minutes";
        public const string BanDurationDays = "comment_ban_duration_days";
    }

    public static class Organization
    {
        public const string StaffInvitationExpiryDays = "staff_invitation_expiry_days";
        public const string InvitationResponseDays = "invitation_response_days";
        public const string MaxTasksPerTeam = "max_tasks_per_team";
        public const string TeamWorkloadWarningThreshold = "team_workload_warning_threshold";
        public const string ContractWarningDays = "contract_warning_days";
    }

    public static class CommunityCleanup
    {
        public const string BeforeImagesMax = "community_before_images_max";
        public const string CheckInReminderMinutesBeforeStart = "check_in_reminder_minutes_before_start";
    }

    public static class DataRetention
    {
        public const string MediaRetentionYears = "media_retention_years";
        public const string AuditLogRetentionMonths = "audit_log_retention_months";
        public const string StatusHistoryRetentionMonths = "status_history_retention_months";
    }

    public static class RateLimits
    {
        public const string SubmitMaxPerHour = "submit_max_per_hour";
        public const string SubmitMaxPerDay = "submit_max_per_day";
        public const string SubmitLockSeconds = "submit_lock_seconds";
    }

    public static class Inspection
    {
        public const string ResolveDaysCritical = "inspection_sla_resolve_days_critical";
        public const string ResolveDaysHigh = "inspection_sla_resolve_days_high";
        public const string ResolveDaysMedium = "inspection_sla_resolve_days_medium";
        public const string ResolveDaysLow = "inspection_sla_resolve_days_low";
        public const string EvidenceMaxPerRequest = "inspection_evidence_max_per_request";
    }

    public static class Ai
    {
        public const string TimeoutSeconds = "ai_timeout_seconds";
        public const string CompareTimeoutSeconds = "ai_compare_timeout_seconds";
        public const string TempImageTtlSeconds = "ai_temp_image_ttl_seconds";
        public const string PresignUploadTtlMinutes = "presign_upload_ttl_minutes";
    }

    public static class Validation
    {
        public const string RejectReasonMinLength = "reject_reason_min_length";
        public const string ReopenReasonMinLength = "reopen_reason_min_length";
        public const string EscalationReasonMinLength = "escalation_reason_min_length";
    }
}

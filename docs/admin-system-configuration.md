# Admin System Configuration — BE Handoff & FE Guide

> **Project:** SU26SE049 GreenLens  
> **Feature:** Hybrid system configuration (`system_settings` table + in-memory cache)  
> **Audience:** Admin Web FE, Mobile (read-only via runtime behavior — no direct admin API)  
> **Last updated:** 2026-09-01 (`title`, `unit`, bounds sync, catalog prune 13 retired keys, geo unbounded max, `max_image_size_mb`)

---

## 1. Tổng quan

Backend đã chuyển các **hằng số nghiệp vụ** (duplicate radius, SLA, rate limit, geo bounds, …) sang bảng `system_settings`, quản trị qua **Admin API**. Giá trị được cache trong memory (`ISystemSettingsProvider`) và **invalidate ngay** sau PATCH/reset.

### Quyết định đã chốt

| Chủ đề | Quyết định |
|--------|------------|
| Hotspot (BR-MAP-010) | **Loại bỏ** — không có key hotspot |
| Seed default | **= hành vi code hiện tại**, không ép theo BR doc |
| Duplicate radius | **25m** (không phải 50m BR) |
| Duplicate time window | **0** = không lọc theo thời gian (hành vi cũ) |
| Auto-close Resolved | **2 ngày** (không phải 7 ngày BR) |
| `recurrence_min_days_after_close` | Logic mới trên submit; default **0** = flag ngay |
| PenaltyFramework | **Giữ CRUD riêng** (`penalty_frameworks`) — không nằm trong system_settings |

### Kiến trúc

```
Admin PATCH → system_settings (DB) → ISystemSettingsCache.RefreshAsync()
                                              ↓
Handlers / Jobs / Validators ← ISystemSettingsProvider (snapshot)
```

- **Seeder:** `SystemSettingsSeeder` — idempotent sync: xóa key retired, thêm key mới, đồng bộ `title`/`description`/`unit`/`minValue`/`maxValue` (**73 keys** active)  
- **Migration:** `202608241700_AddSystemSettings`, `202609011200_AddSystemSettingTitle`, `202609011330_AddSystemSettingUnit`
- **Accessors:** `ReportSystemSettings`, `ModuleSystemSettings` (typed fallbacks)

---

## 2. Admin API

Base path: **`/api/v1/admin/system-settings`**  
Auth: **Admin** role + policy tương ứng trên `AdminController`.

### 2.1 Danh sách module (sidebar)

```http
GET /api/v1/admin/system-settings/modules
```

Response `data.modules[]`:

| Field | Mô tả |
|-------|--------|
| `module` | Enum string (`Reports`, `Sla`, …) |
| `routeSlug` | URL slug (`reports`, `community_cleanup`, …) |
| `displayNameVi` | Nhãn hiển thị |
| `descriptionVi` | Mô tả ngắn |

### 2.2 Lấy settings

```http
GET /api/v1/admin/system-settings?module=reports
GET /api/v1/admin/system-settings/reports
```

Response `data.items[]`:

| Field | Mô tả |
|-------|--------|
| `key` | Snake_case key |
| `title` | Nhãn ngắn tiếng Việt (hiển thị form) |
| `unit` | Đơn vị hiển thị cạnh ô nhập (vd. `m`, `MB`, `ngày`). `null` = không hiện suffix |
| `valueType` | `Int`, `Decimal`, `Bool`, `String`, `Json` |
| `value` | Giá trị hiện tại |
| `defaultValue` | Giá trị seed / reset |
| `description` | Mô tả tiếng Việt (khi áp dụng + hệ thống làm gì) |
| `minValue` / `maxValue` | Nullable — FE validate trước submit. `maxValue: null` = không giới hạn trên (4 key Geo khoảng cách mét) |
| `isActive` | Luôn `true` khi active |

**Quy ước copy:** `title` và `description` 100% tiếng Việt, không gắn mã BR, **không** chứa đơn vị. `title` = tên dễ hiểu; `unit` = đơn vị cạnh input (FE render `[input] m`); `description` = ngữ cảnh áp dụng + hành vi hệ thống.

### 2.3 Cập nhật (bulk theo module)

```http
PATCH /api/v1/admin/system-settings/{module}
Content-Type: application/json

{
  "duplicate_radius_meters": "30",
  "auto_close_resolved_days": "3"
}
```

- Chỉ gửi key **thuộc module** đó  
- BE validate type + min/max → 400 nếu sai  
- Sau PATCH cache refresh + audit log (`BR-ADM-010`)

### 2.4 Reset module về default

```http
POST /api/v1/admin/system-settings/{module}/reset
```

Reset **toàn bộ key** trong module về `defaultValue` từ seeder.

---

## 3. Catalog keys theo module

### Reports (`reports`)

| Key | Default | Min–Max | Ghi chú |
|-----|---------|---------|---------|
| `duplicate_radius_meters` | **25** | 10–500 | Tier-1 duplicate |
| `duplicate_time_window_hours` | **0** | 0–8760 | 0 = không lọc thời gian |
| `duplicate_max_candidates` | 20 | 5–100 | |
| `duplicate_merge_points_ratio` | 0.5 | 0–1 | Điểm khi gộp trùng |
| `recurrence_radius_meters` | 25 | 10–500 | Tái phạm |
| `recurrence_min_days_after_close` | **0** | 0–365 | 0 = flag ngay sau Closed |
| `recurrence_max_days_after_close` | 30 | 1–365 | |
| `max_images_per_report` | 5 | 1–10 | |
| `max_image_size_mb` | **10** | 1–50 | Admin nhập MB; runtime quy đổi sang bytes |
| `auto_close_resolved_days` | **2** | 1–30 | Chờ xác nhận citizen |
| `reopen_window_days` | 7 | 1–90 | |
| `max_approved_reopens` | 1 | 0–5 | |

**Hardcode (không còn trong system_settings):** `max_drafts_per_user` = 3, `flag_notify_threshold` = 3, `draft_retention_days` = 7 (job dọn nháp), lý do escalate cleanup team → LEO = **20 ký tự** (hardcode validator, không config).

### SLA (`sla`)

| Key | Default |
|-----|---------|
| `sla_verify_hours` | 24 |
| `sla_resolve_days_critical/high/medium/low` | 3 / 5 / 7 / 10 |
| `overdue_pending_hours` | 72 |
| `unassigned_verified_hours` | 24 |

### Geo (`geo`)

| Key | Default | Min–Max | Ghi chú |
|-----|---------|---------|---------|
| `vietnam_min/max_latitude` | 8 / 24 | 0–90 | |
| `vietnam_min/max_longitude` | 102 / 110 | 0–180 | |
| `check_in_max_distance_meters` | 200 | 50 – **∞** | `maxValue: null` |
| `exif_gps_mismatch_meters` | 200 | 50 – **∞** | `maxValue: null` |
| `inspection_soft_gps_meters` | 200 | 50 – **∞** | `maxValue: null` |
| `progress_update_max_distance_meters` | 200 | 50 – **∞** | `maxValue: null` |

4 key khoảng cách mét: admin nhập tùy ý ≥ min; BE/FE không giới hạn trần (`maxValue: null`).

### Map (`map`) — **không hotspot**

| Key | Default |
|-----|---------|
| `public_coordinate_decimal_places` | 4 |
| `map_max_bounding_lat/lng_span` | 6 / 8 |
| `map_default/max_detail_limit` | 200 / 500 |
| `map_default_grid_level` | 3 |
| `map_viewport_min/max_days` | 7 / 90 | Default query param `days=30` hardcode trong API |
| `map_max_aggregate_rows` | 50000 |

### Officer (`officer`)

| Key | Default | Công thức priority |
|-----|---------|-------------------|
| `priority_severity_weight` | 3 | `severity×W1 + reporters×W2 + ageHours/W3` |
| `priority_reporter_count_weight` | 2 | |
| `priority_age_divisor_hours` | 24 | |
| `priority_sla_verify_breach_boost` | 100 | Cộng khi breach verify |

### Cleanup (`cleanup`)

| Key | Default | Ghi chú |
|-----|---------|---------|
| `progress_stale_hours` | 24 | Nhắc đội dọn dẹp |
| `progress_escalate_hours` | 48 | Cảnh báo **LEO phường** (không escalate lên DEO) |
| `decline_window_hours` | 24 | Cửa sổ từ chối nhiệm vụ |

### Notifications (`notifications`)

| Key | Default |
|-----|---------|
| `nearby_report_radius_meters` | 2000 |
| `nearby_report_max_recipients` | 100 |
| `max_notifications_per_type_per_day` | 20 |

### Auth (`auth`)

| Key | Default |
|-----|---------|
| `max_failed_login_attempts` | 5 |
| `lockout_minutes` | 30 |
| `otp_max_attempts` | 5 |
| `account_soft_delete_retention_days` | 90 |

### Organization (`organization`)

| Key | Default | Ghi chú |
|-----|---------|---------|
| `staff_invitation_expiry_days` | 7 | Hết hạn lời mời |
| `invitation_response_days` | 7 | Placeholder template lời mời |
| `contract_warning_days` | **`[30,7,1]`** (Json) | Job cảnh báo hết hạn HĐ + DEO alert horizon |

**Workload đội dọn dẹp:** đọc từ `WorkloadLimitsOptions` (`appsettings`) — không qua system_settings.

JSON `contract_warning_days`: mảng số nguyên dương (ngày trước `ContractEndDate`). BE parse, loại ≤0, distinct, sort desc. DEO dashboard dùng **max** phần tử làm “trong X ngày”.

### AI (`ai`)

| Key | Default | Min–Max | Consumer |
|-----|---------|---------|----------|
| `ai_timeout_seconds` | **30** | 1–60 | `AiClassificationService` |
| `ai_compare_timeout_seconds` | 15 | 1–120 | `AiImageCompareService` |
| `ai_temp_image_ttl_seconds` | 900 (15 phút) | 60–3600 | `TempImageStore` |
| `presign_upload_ttl_minutes` | 15 | 1–120 | `PresignMediaUploadCommandHandler` |

`AiOptions.BaseUrl` vẫn đọc từ `appsettings` / user-secrets — **không** nằm trong system_settings.

### Comments (`comments`)

| Key | Default | Min–Max |
|-----|---------|---------|
| `comment_edit_window_minutes` | 15 | 1–1440 |
| `comment_ban_duration_days` | 7 | 1–90 |

### Community cleanup (`community_cleanup`)

| Key | Default | Min–Max |
|-----|---------|---------|
| `community_before_images_max` | 5 | 1–20 |
| `check_in_reminder_minutes_before_start` | 15 | 5–120 |

### Data retention (`data_retention`)

| Key | Default | Min–Max |
|-----|---------|---------|
| `media_retention_years` | 2 | 1–10 |
| `audit_log_retention_months` | 12 | 6–60 |
| `status_history_retention_months` | 12 | 6–60 |

### Rate limits (`rate_limits`)

| Key | Default | Min–Max |
|-----|---------|---------|
| `submit_max_per_hour` | 5 | 1–50 |
| `submit_max_per_day` | 20 | 1–100 |
| `submit_lock_seconds` | 3600 | 60–86400 |

### Inspection (`inspection`)

| Key | Default | Min–Max |
|-----|---------|---------|
| `inspection_sla_resolve_days_critical/high/medium/low` | 3 / 5 / 7 / 10 | 1–30 (low: 1–60) |

**Chưa wire:** `inspection_evidence_max_per_request` (retired khỏi catalog).

### Validation (`validation`)

| Key | Default | Min–Max |
|-----|---------|---------|
| `reject_reason_min_length` | 20 | 5–500 |
| `reopen_reason_min_length` | 20 | 5–500 |

**Retired:** `escalation_reason_min_length` (LEO→DEO escalate đã gỡ; cleanup escalate hardcode 20 ký tự).

### Key đã gỡ khỏi catalog (13 — seeder xóa DB)

| Module | Key | Lý do |
|--------|-----|--------|
| `reports` | `recurrence_lookback_days` | Không dùng |
| `reports` | `max_image_size_bytes` | → `max_image_size_mb` |
| `reports` | `max_drafts_per_user` | Hardcode 3 |
| `reports` | `draft_retention_days` | Hardcode 7 ngày |
| `reports` | `flag_notify_threshold` | Hardcode 3 |
| `sla` | `sla_verify_breach_priority_boost` | Trùng officer key |
| `map` | `map_viewport_default_days` | Default `days=30` hardcode API |
| `cleanup` | `progress_update_interval_hours` | Chưa implement |
| `auth` | `captcha_after_failed_attempts` | CAPTCHA chưa implement |
| `organization` | `max_tasks_per_team` | `WorkloadLimitsOptions` |
| `organization` | `team_workload_warning_threshold` | `WorkloadLimitsOptions` |
| `inspection` | `inspection_evidence_max_per_request` | Chưa implement |
| `validation` | `escalation_reason_min_length` | LEO→DEO escalate gỡ |

**Gamification module** có trong sidebar catalog nhưng **0 key** trong seeder (GET trả rỗng). **Ngưỡng badge** quản lý riêng qua `PATCH /v1/admin/badges/{id}/thresholds` (xem `docs/api-admin-module.md` §5.3).

**Nguồn đầy đủ:** `SystemSettingDefinitions.cs`, `SystemSettingKeys.cs`.

---

## 4. Consumers đã wire

| Module | Handlers / Jobs / Services |
|--------|----------------------------|
| Reports | Submit, SaveDraft, Flag, Reopen, Duplicate merge points, AutoClose, DraftCleanup, PossibleDuplicate (radius) |
| SLA | Report/Inspection SLA policy, SlaBreach*, OverdueReportJob, PriorityScore |
| Geo | Submit validator, check-in handlers, EXIF analyzer, map validators |
| Map | GetPublicMapReports, GetMapViewportSummary |
| Cleanup | CleanupProgressSlaJob, DeclineAssignment |
| Rate limits | Redis + InMemory submission limiter |
| Notifications | Anti-spam (`max_notifications_per_type_per_day`), NearbyReport, Case B placeholders, Case C templates |
| Auth | Login lockout, AccountHardDelete, **VerifyOtp / ResetPassword (`otp_max_attempts`)** |
| Comments | Edit/Delete/Add + ban |
| Org | StaffInvitation expiry, RecruitStaff template, **`CompanyContractExpiryJob` + DEO contract alert (`contract_warning_days`)** |
| AI | **Classify, compare, temp image TTL, presign upload TTL** |
| Analytics | **GetAdminAlerts / GetDeoAlerts — message text `overdue_pending_hours`, contract horizon** |
| Admin | **GetSpamSuspects — default `minReportsPerHour` ← `submit_max_per_hour` khi query param omitted** |
| Retention | DataRetentionJob |
| Validation | Reject, Reopen, Decline reason min length (reject/reopen từ settings; decline dùng reject min) |
| Media | PresignMediaUpload (TTL) |

### 4.1 Luồng notification — admin config có hiệu lực?

| Path | Config áp dụng? |
|------|-----------------|
| `SendFromTemplateAsync` | Có — merge `NotificationSystemSettingPlaceholders` + anti-spam limit |
| Background jobs (AutoClose, Overdue, SlaBreach*, …) | Có — qua template + merge |
| Flow handlers (submit, assign, comment, org, …) | Có — qua template |
| `NearbyReportNotificationHandler` | Có — radius/max recipients từ settings |
| `PossibleDuplicateFlaggedNotificationHandler` | Có — radius trong `detection_summary` |
| Admin test template | Có — handler merge settings trước `SendRawAsync` |
| `detection_summary` / `activity_summary` (C# build) | **Khung câu hardcode BE** — chỉ **số** lấy từ settings |

### 4.2 Text hardcode còn lại (BE) — không qua template/settings

| Vị trí | Xử lý đề xuất |
|--------|----------------|
| `Errors.*`, success message OTP | Giữ hardcode hoặc `.resx` i18n — **không** admin config |
| `NotificationPlaceholders` khung câu tiếng Việt | Giữ; số đã từ settings |
| Dashboard alert message (Admin/DEO) | **Đã wire số** (`overdue_pending_hours`, `contract_alert_horizon`) — câu vẫn string BE |
| `max_tasks_per_team`, `team_workload_warning_threshold` | `WorkloadLimitsOptions` (appsettings) |
| `captcha_after_failed_attempts` | Stub đã gỡ — chưa triển khai CAPTCHA |

**FE Admin UI:** label lấy từ `title`; tooltip/helper lấy từ `description` API; **không** hardcode số nghiệp vụ làm source of truth.

### Notification templates mới (thay `SendRawAsync`)

| Type | Template key | Placeholder |
|------|--------------|-------------|
| `ReportDuplicateMerged` | `report_duplicate_merged` | `{report_code}` |
| `CompanyDeactivationReassign` | `company_deactivation_reassign` | `{report_code}` |

### Case B — placeholder config-driven (PR riêng)

8 template SLA/lifecycle/nearby/invitation/reminder dùng placeholder thay số cứng (`{sla_verify_hours}`, `{auto_close_resolved_days}`, …). Giá trị merge tự động trong `NotificationService.SendFromTemplateAsync` qua `NotificationSystemSettingPlaceholders` — admin đổi System Settings thì message gửi ra khớp ngay, không cần sửa template.

| Template key | Placeholders |
|--------------|--------------|
| `sla_verification_breached_leo` | `{sla_verify_hours}` |
| `report_overdue` | `{overdue_pending_hours}` |
| `report_unassigned` | `{unassigned_verified_hours}` |
| `report_auto_closed` | `{auto_close_resolved_days}` |
| `cleanup_progress_stale` | `{progress_escalate_hours}` |
| `nearby_report` | `{nearby_radius_km}` |
| `staff_invitation_received` | `{invitation_response_days}` |
| `community_cleanup_checkin_reminder` | `{check_in_reminder_minutes}` |

`SyncTemplateBodiesAsync` cập nhật DB khi deploy/chạy seed.

Admin vẫn chỉnh **câu chữ** template qua Notification Templates module.

### PenaltyFramework (ngoài scope system_settings)

- CRUD `penalty_frameworks` — Admin quản lý khung phạt theo category/level  
- `IssuePenaltyCommandHandler` đọc từ DB entity, không qua `system_settings`  
- Inspection SLA days **đã** config qua module `inspection`

---

## 5. Gợi ý giao diện Admin FE

### 5.1 Navigation

```
Cấu hình hệ thống
├── Báo cáo          (/admin/settings/reports)
├── SLA
├── Địa lý
├── Bản đồ công khai
├── … (17 module)
└── Validation
```

- Load sidebar từ `GET .../modules`  
- Route param = `routeSlug` (vd. `community_cleanup`)

### 5.2 Form layout

Mỗi module = **1 trang form** grouped theo nhóm con (accordion):

**Reports example:**
- Nhóm **Trùng lặp**: radius, time window, max candidates, merge ratio  
- Nhóm **Tái phạm**: radius, min/max days after close  
- Nhóm **Vòng đời**: auto-close, reopen, images (MB)

**Control theo `valueType`:**

| Type | UI control |
|------|------------|
| `Int` | Number input + `unit` suffix (vd. `200` + `m`) |
| `Decimal` | Number input (step 0.01) + `unit` suffix |
| `Bool` | Switch |
| `String` | Text input |
| `Json` | JSON editor (vd. `contract_warning_days`: `[30,7,1]`) |

### 5.3 UX patterns

1. **Hiển thị default:** badge “Mặc định: 25” cạnh field; nút “Khôi phục” từng field về `defaultValue`  
2. **Dirty state:** PATCH chỉ gửi key đã đổi  
3. **Reset module:** modal xác nhận → `POST .../reset`  
4. **Validation client:** dùng `minValue`/`maxValue` từ API; **`maxValue: null`** = không giới hạn trên (4 key Geo khoảng cách — xem bảng Geo §3); hiển thị lỗi BE ProblemDetails  
5. **Tooltip:** `description` làm helper text; **label field** dùng `title`; **suffix input** dùng `unit` (ẩn nếu `null`)
6. **Không hiển thị hotspot** — module đã bỏ  
7. **Cảnh báo thay đổi SLA/rate limit:** banner “Ảnh hưởng job Hangfire & queue — có hiệu lực sau vài giây (cache refresh)”
8. **Xác nhận trước khi áp dụng (bắt buộc):** Khi admin sửa một hoặc nhiều giá trị rồi bấm **Áp dụng / Lưu**, FE **phải** hiện dialog xác nhận trước khi gọi API — không submit thẳng. Dialog nên:
   - Tiêu đề dạng: *“Xác nhận thay đổi cấu hình?”*
   - Nội dung: liệt kê ngắn các field đã đổi (key/label + **giá trị cũ → giá trị mới**)
   - Hai nút: **Hủy** (đóng dialog, giữ dirty state) và **Xác nhận áp dụng** (mới gọi `PATCH` hoặc `PATCH .../thresholds`)
   - Áp dụng cho **cả** System Settings module lẫn **ngưỡng badge** (`PATCH /v1/admin/badges/{id}/thresholds`)
   - Với thay đổi nhạy cảm (SLA, rate limit, auto-close, ngưỡng badge milestone), có thể thêm dòng cảnh báo: *“Thay đổi có hiệu lực ngay với user mới / hành vi tương lai; badge đã cấp không bị thu hồi.”*

### 5.4 Badge thresholds (ngoài system_settings)

- Màn **Quản lý huy hiệu**: GET `/v1/admin/badges` → một input `threshold` theo từng badge (label động theo `code`, xem `docs/api-admin-module.md` §5.3)
- Bấm lưu ngưỡng → **cùng pattern dialog xác nhận** mục 5.3 mục 8 (vd. *“Đổi ngưỡng Eco Warrior: 10 → 15 báo cáo verified?”*)
- PUT `/v1/admin/badges/{id}` chỉ cho tên/mô tả/icon — không trộn với form ngưỡng

### 5.5 Mobile / Citizen app

Không gọi Admin API. Hành vi thay đổi **ngầm** (vd. submit bị rate limit, map bbox nhỏ hơn). Không cần UI config.

### 5.6 LEO / Officer web

Tùy chọn: hiển thị read-only SLA due dates từ report API (đã tính từ config). Không cần màn settings.

---

## 6. Lộ trình PR & commit messages

Chia **6 PR** để review dễ. User tự commit theo từng PR.

### PR-1 — Foundation

```
feat(admin): add SystemSetting entity, seeder, provider cache, and admin API (BR-ADM-010)

Hybrid system configuration foundation: system_settings table, idempotent seeder
with current-code defaults, ISystemSettingsProvider + cache invalidation,
GET/PATCH/reset admin endpoints under v1/admin/system-settings.
```

### PR-2 — Reports duplicate / recurrence / lifecycle

```
feat(reports): wire duplicate, recurrence, and lifecycle to SystemSetting (BR-REP-030, BR-REP-034, BR-REP-019)

Refactor submit duplicate/recurrence detection, auto-close, drafts, reopen limits,
flag threshold, and merge points ratio to read ISystemSettingsProvider.
Implement recurrence_min_days_after_close filter on submit.
```

### PR-3 — SLA, geo, priority, cleanup, rate limits

```
feat(infra): wire SLA, geo, officer priority, cleanup SLA, and submit rate limits to SystemSetting

Report/Inspection SLA policies, priority jobs, overdue notifications, check-in distance,
Vietnam bounds validators, and Redis/in-memory submission rate limiters read config cache.
```

### PR-4 — Map, notifications, templates (Case C)

```
feat(map,notifications): wire public map limits and migrate raw notifications to templates

Map viewport/detail limits and coordinate rounding from settings; anti-spam and nearby radius;
ReportDuplicateMerged and CompanyDeactivationReassign template types (no hotspot config).
```

### PR-7 — Notification template Case B (config placeholders)

```
feat(notifications): replace hardcoded numeric literals in templates with system-setting placeholders (BR-NTF-002)

Add NotificationSystemSettingPlaceholders auto-merge on SendFromTemplateAsync;
update 8 SLA/lifecycle/nearby/invitation templates; sync seeder bodies via SyncTemplateBodiesAsync.
```

### PR-5 — Auth, comments, org, community, retention, validation

```
feat(auth,comments,org): wire auth lockout, comments, invitations, retention, and validation lengths

Login lockout/CAPTCHA thresholds, comment edit window, staff invitation expiry,
data retention jobs, and minimum reason lengths driven by system settings.
```

### PR-6 — PenaltyFramework note + docs

```
docs(admin): add system configuration handoff and FE guide

Document admin API, key catalog, seed-vs-BR deviations, wired consumers,
and FE UI patterns. PenaltyFramework remains separate DB catalog.
```

### PR-8 — AI timeouts, presign TTL, OTP max attempts

```
feat(infra): wire AI timeouts, temp image TTL, presign TTL, and OTP max attempts to SystemSetting (BR-AI-006, BR-AUTH-011)

ModuleSystemSettings.Ai accessor; AiClassificationService, AiImageCompareService,
TempImageStore, PresignMediaUploadCommandHandler; OtpCode configurable max attempts
in VerifyOtp and ResetPassword handlers.
```

### PR-9 — Contract warning, overdue alert text, spam default

```
feat(org,analytics,admin): wire contract warning days, overdue alert text, and spam threshold to SystemSetting (BR-CMP-007, BR-REP-008, BR-ADM-007)

CompanyContractExpiryJob and GetDeoAlerts use contract_warning_days JSON;
GetAdminAlerts/GetDeoAlerts overdue message uses overdue_pending_hours;
GetSpamSuspects default minReportsPerHour from submit_max_per_hour when omitted.
```

---

## 7. Kiểm thử

```bash
dotnet build
dotnet test tests/Greenlens.Application.UnitTests
dotnet test tests/Greenlens.Domain.UnitTests
```

Sau deploy staging:
1. PATCH `duplicate_radius_meters` → submit 2 report gần nhau → verify duplicate behavior  
2. PATCH `auto_close_resolved_days` → chờ job hoặc trigger manual  
3. PATCH rate limit → verify 429/lock message  
4. PATCH `ai_timeout_seconds` → classify timeout log/thời gian chờ  
5. PATCH `otp_max_attempts` → verify OTP lockout sau N lần sai  
6. PATCH `contract_warning_days` → `[14,3,1]` → job warning + DEO alert text “14 ngày”  
7. PATCH `overdue_pending_hours` → message alert Admin/DEO khớp số mới  
8. PATCH `submit_max_per_hour` → `GET /spam-suspects` (không `minReportsPerHour`) dùng ngưỡng mới  
9. Reset module → values = defaultValue

### Spam dashboard API note

```http
GET /api/v1/admin/spam-suspects?page=1&pageSize=20
```

- **Không** truyền `minReportsPerHour` → BE dùng `submit_max_per_hour` từ settings  
- Truyền `minReportsPerHour=10` → override explicit cho lần query đó

---

## 8. Liên hệ code

| Thành phần | Path |
|------------|------|
| Entity | `src/Greenlens.Domain/Entities/SystemSetting.cs` |
| Seeder | `src/Greenlens.Infrastructure/Seeders/Administrator/SystemSettingDefinitions.cs` |
| Provider | `src/Greenlens.Infrastructure/Configuration/SystemSettingsProvider.cs` |
| Keys | `src/Greenlens.Application/BusinessRules/SystemSettingKeys.cs` |
| Accessors | `ReportSystemSettings.cs`, `ModuleSystemSettings.cs` |
| Notification placeholders | `NotificationSystemSettingPlaceholders.cs` |
| Admin API | `src/Greenlens.Api/Controllers/AdminController.cs` |

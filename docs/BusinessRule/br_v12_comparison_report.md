# Báo cáo: BR v1.2 ↔ OVERVIEW.md ↔ Codebase

> **Source of truth:** [SU26SE049_BusinessRules_v1_2.md](file:///d:/LEARNING/S9SU26/SEP490/greenlens-service/SU26SE049_BusinessRules_v1_2.md) (v1.2, 07/06/2026)
> **OVERVIEW hiện tại:** [OVERVIEW.md](file:///d:/LEARNING/S9SU26/SEP490/greenlens-service/OVERVIEW.md) v1.5
> **Cập nhật báo cáo:** 2026-07-15 — Comments, Duplicate Detection, AiRetryJob

---

# Phần A — BR v1.2 vs OVERVIEW.md (Những gì OVERVIEW thiếu/sai)

OVERVIEW.md v1.5 tuyên bố đã "Đồng bộ với SU26SE049_BusinessRules_v1_2.docx". Tuy nhiên, nhiều BR ID mới trong v1.2 **KHÔNG được reference** trong OVERVIEW:

## A.1 BR IDs thiếu hoàn toàn trong OVERVIEW §5

| BR ID          | Nội dung                                              |                       Ưu tiên bổ sung                        |
| -------------- | ----------------------------------------------------- | :----------------------------------------------------------: |
| **BR-ORG-005** | Mô hình đa đơn vị trong 1 phường                      | **Cao** — OVERVIEW nêu ý tưởng (§1.1F) nhưng không tag BR ID |
| **BR-ORG-020** | Mời thành viên đội cộng đồng (LEO mời qua email)      |                   **Cao** — flow mới v1.2                    |
| **BR-ORG-021** | Hiệu lực lời mời (7 ngày, single-use)                 |                           **Cao**                            |
| **BR-OFF-005** | Triage: dọn dẹp & xử phạt song song                   |  Có nhắc (line 588) nhưng ghi **"v1.3"** thay vì **"v1.2"**  |
| **BR-CLN-008** | Đội Company: CM phân công, kiểm tra hiệu lực hợp đồng |                        **Trung bình**                        |
| **BR-CMP-006** | Gia hạn / tái ký hợp đồng                             |                        **Trung bình**                        |
| **BR-CMP-010** | CM thêm nhân viên (mời email)                         |                           **Cao**                            |
| **BR-CMP-011** | CM lập & điều phối đội                                |        **Cao** — đã có code nhưng OVERVIEW không tag         |
| **BR-CMP-012** | Phạm vi phục vụ (N-N check)                           |                 Có gián tiếp qua BR-CMP-014                  |
| **BR-CMP-013** | Vô hiệu hóa kế thừa (suspend → staff mất quyền)       |               **Cao** — critical business rule               |
| **BR-CMP-020** | KPI công ty                                           |                        **Trung bình**                        |
| **BR-CMP-021** | Phân tách dữ liệu công ty                             |                        **Trung bình**                        |
| **BR-INS-002** | Chỉ xem task được gán (scope check)                   |                     **Thấp** — implicit                      |
| **BR-INS-003** | Từ chối task 2h                                       |                        **Trung bình**                        |
| **BR-INS-004** | Check-in ≤ 200m                                       |                        **Trung bình**                        |
| **BR-INS-030** | SLA Inspection = SLA Cleanup                          |                 **Cao** — SLA chưa implement                 |
| **BR-INS-031** | Update tiến độ ≥ 1/ngày                               |                        **Trung bình**                        |
| **BR-INS-032** | KPI Inspection Team                                   |                        **Trung bình**                        |
| **BR-ADM-012** | Giám sát công ty (Admin toàn quốc, DEO theo tỉnh)     |                        **Trung bình**                        |

## A.2 Sai lệch cần sửa

| Vị trí OVERVIEW              | Sai                                                          | Đúng (BR v1.2)                                                                      |
| ---------------------------- | ------------------------------------------------------------ | ----------------------------------------------------------------------------------- |
| Line 21, 549                 | Ghi **"v1.3"**                                               | Phải là **"v1.2"** — tài liệu BR chỉ có v1.2                                        |
| Line 581                     | `BR-AUTH-022` (xóa tài khoản)                                | BR v1.2 đánh số là **`BR-AUTH-021`**                                                |
| Line 33 (CompanyStatus enum) | Có 4 status: `PendingActivation, Active, Suspended, Expired` | BR-CMP-004 yêu cầu **5 status**: thêm **`Terminated`**                              |
| §5 BR mapping table          | Không mention BR-REP-005 giảm từ 5 → 3 loại                  | BR v1.2 chỉ còn **3 loại** (Rác thải, Nước thải, Hóa chất) — bỏ Không khí, Tiếng ồn |

## A.3 Thiếu ý quan trọng trong OVERVIEW

| Ý trong BR v1.2 | Mô tả                                                  | Section cần bổ sung                                            |
| --------------- | ------------------------------------------------------ | -------------------------------------------------------------- |
| **BR-ORG-016**  | Escalation tuyến cấp TP (cờ admin cấu hình)            | §5 State Machine — đã nhắc nhưng chưa đủ chi tiết về cơ chế cờ |
| **BR-REP-018**  | Citizen đánh giá sau Resolved                          | Chưa mention                                                   |
| **BR-CMP-013**  | Khi Suspend/Terminate company → push tasks về LEO      | Chưa mention — critical cho task reassignment                  |
| **BR-INS-022**  | Tái phạm (≥ 2 biên bản / 12 tháng → nâng mức phạt)     | Chưa mention — business logic phức tạp                         |
| **BR-REP-033**  | Flag duplicate bởi người dùng (≥ 3 flag → LEO xem xét) | ✅ Codebase: `FlagReport/` + template `duplicate_review_needed` — OVERVIEW chưa mention |

---

# Phần B — BR v1.2 vs Codebase (Đã làm / Chưa làm)

## Tổng quan nhanh

|      Trạng thái       | Modules                                                                                                                                                                                                 | % ước tính |
| :-------------------: | :---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :--------: |
|  ✅ Core hoàn thành   | Auth, Reports (36+ slices), Comments, Duplicate Detection, Organization, Inspection, Cleanup, Company, Admin, Gamification, Notifications, Map (2), Catalog, Media, Users |   ~89%     |
| ⚠️ Implement một phần | Map (6 rules còn lại), AI (BR-AI-002/004/005/007), Global API rate limit middleware (BR-SYS-004)                                                                      |    ~6%     |
|   ❌ Chưa implement   | Brute-force + CAPTCHA (BR-AUTH-014)                                                                                                                                    |    ~3%     |

> **Cập nhật 2026-07-15:** BR-REP-004 (mô tả 10–1000 + profanity), BR-REP-010 (Redis rate limit 5/h, 20/24h), BR-REP-011 (EXIF stale → Suspicious). Comments, Duplicate, AiRetryJob.

---

## B.1 Auth & Account (`BR-AUTH-001..021`) — ✅ 15/17 rules

| BR          | Mô tả                                      | Status | Evidence                                                                 |
| ----------- | ------------------------------------------ | :----: | ------------------------------------------------------------------------ |
| BR-AUTH-001 | Email RFC 5322                             |   ✅   | `Register/` validator                                                    |
| BR-AUTH-002 | Email unique                               |   ✅   | `Register/` handler                                                      |
| BR-AUTH-003 | SĐT VN format                              |   ✅   | `Register/` validator                                                    |
| BR-AUTH-004 | SĐT unique                                 |   ✅   | `Register/` handler                                                      |
| BR-AUTH-005 | Password strength                          |   ✅   | `Register/` validator                                                    |
| BR-AUTH-006 | Confirm password                           |   ✅   | `Register/` validator                                                    |
| BR-AUTH-007 | OTP 10 phút                                |   ✅   | `RequestOtp/`, `VerifyOtp/`                                              |
| BR-AUTH-008 | Default role Citizen                       |   ✅   | `Register/` handler                                                      |
| BR-AUTH-009 | Phân cấp quyền tạo role (Admin/DEO/LEO/CM) |   ✅   | `UpdateUserRole/` handler — Admin only, DEO/LEO/CM dùng flow riêng       |
| BR-AUTH-010 | Required fields                            |   ✅   | `Register/` validator                                                    |
| BR-AUTH-011 | Tên 2-50 ký tự tiếng Việt                  |   ✅   | `Register/` validator — regex `[\p{L}\s]` 2-50 chars                     |
| BR-AUTH-012 | Accept Terms                               |   ✅   | `RegisterCommand.AcceptTerms` + validator `.Equal(true)`                 |
| BR-AUTH-013 | Login email/SĐT                            |   ✅   | `Login/` feature                                                         |
| BR-AUTH-014 | Brute-force lock 30' + CAPTCHA lần 3       |   ❌   | Chưa có sliding window + Turnstile                                       |
| BR-AUTH-015 | Block Inactive/Banned + Expired company    |   ✅   | Login check `IsBanned`, `IsDeleted`, company `Expired` + `ToggleBanUser` |
| BR-AUTH-016 | JWT 24h + Refresh 30d                      |   ✅   | `RefreshToken/`, `JwtService.cs`                                         |
| BR-AUTH-017 | Guest read-only (bỏ anonymous submit)      |   ✅   | `[Authorize]` trên submit endpoints                                      |
| BR-AUTH-018 | Forgot/Reset password                      |   ✅   | `ForgotPassword/`, `ResetPassword/`                                      |
| BR-AUTH-019 | Update profile                             |   ✅   | `Users/UpdateUserProfile/`                                               |
| BR-AUTH-020 | Change password (không trùng 3 MK cũ)      |   ✅   | `ChangePassword/` + `PasswordHistory` entity, check last 3 hashes        |
| BR-AUTH-021 | Xóa tài khoản soft delete 90d              |   ✅   | `RequestAccountDeletion/`, `RestoreAccount/`, `AccountHardDeleteJob`     |

---

## B.2 Organization & Routing (`BR-ORG-001..021`) — ✅ 11/11 rules

| BR         | Mô tả                                                       | Status | Evidence                                                                                                                                 |
| ---------- | ----------------------------------------------------------- | :----: | ---------------------------------------------------------------------------------------------------------------------------------------- |
| BR-ORG-001 | Department cấp Tỉnh                                         |   ✅   | `CreateDepartment/`, `GetDepartments/`                                                                                                   |
| BR-ORG-002 | Local Office cấp Xã/Phường                                  |   ✅   | `CreateLocalOffice/`, CRUD                                                                                                               |
| BR-ORG-003 | Office → Team (1:N)                                         |   ✅   | `CreateTeam/`, `AddTeamMember/`, `RemoveTeamMember/`                                                                                     |
| BR-ORG-004 | Ward polygon geo routing                                    |   ✅   | `Ward.cs`, PostGIS                                                                                                                       |
| BR-ORG-005 | Đa đơn vị trong 1 phường                                    |   ✅   | N-N model `CompanyServiceArea`                                                                                                           |
| BR-ORG-010 | GPS → Ward → LEO routing                                    |   ✅   | `SubmitPollutionReport/` handler                                                                                                         |
| BR-ORG-011 | Department Common Queue                                     |   ✅   | `SubmitPollutionReport/` handler: ward chưa onboard → `RouteToDepartment(dept.Id)`                                                       |
| BR-ORG-012 | Conflict of interest (LEO ≠ reporter + ward scope)          |   ✅   | `VerifyReport/`: `ConflictOfInterest` (self) + `OutsideJurisdiction` (ngoài phường)                                                      |
| BR-ORG-013 | Quyết định xử lý khi xác minh (dọn dẹp + xử phạt song song) |   ✅   | `VerifyReport/` → verify, `AssignTeam/` → cleanup, `CreateInspectionReport/` → xử phạt (2 nhánh độc lập)                                 |
| BR-ORG-014 | SLA tiếp nhận 24h → escalate DEO                            |   ✅   | `SlaBreachVerificationJob`: flag `SlaVerifyBreached` + `EscalateToDepartment()` (clear AssignedOfficeId → DEO queue)                     |
| BR-ORG-015 | Re-assign khi LEO reject                                    |   ✅   | `RejectReport/`: reason ≥ 20 chars, status stays Submitted, AssignedOfficeId cleared → Department queue                                  |
| BR-ORG-016 | Escalation tuyến cấp TP                                     |   ✅   | `EscalateReport/`: LEO manually escalate Verified/InProgress → DEO queue (clear AssignedOfficeId). Endpoint `POST reports/{id}/escalate` |
| BR-ORG-020 | Mời thành viên đội (LEO mời qua email)                      |   ✅   | `RecruitStaff/` (invitation flow), `LookupCitizenByEmail/`, `AcceptInvitation/`, `DeclineInvitation/`                                    |
| BR-ORG-021 | Hiệu lực lời mời 7 ngày                                     |   ✅   | `StaffInvitation` entity (7d expiry, single-use), `GetMyInvitations/`, `ReleaseStaff/`                                                   |

---

## B.3 Report (`BR-REP-001..033`) — ✅ 21/23 rules

| BR              | Mô tả                                    | Status | Evidence                                                                          |
| --------------- | ---------------------------------------- | :----: | --------------------------------------------------------------------------------- |
| BR-REP-001      | Ảnh 1-5, ≤ 10MB                          |   ✅   | Validator + `UploadReportImage/`                                                  |
| BR-REP-002      | Video 1, mp4/mov                         |   ✅   | `UploadReportVideo/` + `FFmpegVideoTranscoder` (H.264 720p CRF 28, max 100MB/60s) |
| BR-REP-003      | GPS Vietnam bounds                       |   ✅   | Validator                                                                         |
| BR-REP-004      | Mô tả: 10–1000 ký tự + filter tục tĩu   |   ✅   | `IProfanityFilter` + Admin CRUD `blocked_words` (`/v1/admin/blocked-words`) |
| BR-REP-005      | **3 loại** ô nhiễm (Rác, Nước, Hóa chất) |   ✅   | `PollutionCategorySeeder` seed 3 loại, SMOKE deactivated                          |
| BR-REP-006      | Severity 4 mức                           |   ✅   | `Severity.cs` enum                                                                |
| BR-REP-008      | Cảnh báo tồn đọng 72h                    |   ✅   | `OverdueReportNotificationJob` + `IsOverdue` flag                                 |
| BR-REP-009      | Cảnh báo chưa phân công 24h              |   ✅   | `OverdueReportNotificationJob` (dedup 24h notification)                           |
| BR-REP-010      | Rate limit 5/h, 20/24h                   |   ✅   | `RedisReportSubmissionRateLimiter` (fallback in-memory khi không có Redis)       |
| BR-REP-011      | EXIF metadata validation                 |   ✅   | `MetadataExtractorImageExifAnalyzer` → `FlagSuspicious` + `ExifWarning` response   |
| BR-REP-012      | Bắt buộc đăng nhập + tùy chọn ẩn tên     |   ✅   | `LoginRequired` + `HideReporterName` trên submit (BR-REP-012 + comment guard CMT-001) |
| BR-REP-013      | Initial status = Submitted               |   ✅   | `Report.cs` factory                                                               |
| BR-REP-014      | Ảnh before/after khi Resolved            |   ✅   | `UploadBeforeImages/` + enforce ≥ 1 before trên `ResolveReportHandler`            |
| BR-REP-015      | Citizen xác nhận (7d, max 2 re-open)     |   ✅   | `TryReopen()` 7-day window + max 2. `ReopenWindowExpired` error                   |
| BR-REP-016      | Auto-close 7 ngày                        |   ✅   | `AutoCloseResolvedReportJob` + StatusHistory + Notification                       |
| BR-REP-017      | Không xóa report đã verified             |   ✅   | `DeleteReport/` + `CanDelete()` guard (Submitted only, no AI/Officer)             |
| BR-REP-018      | Đánh giá của Citizen sau Resolved        |   ✅   | `RateReport/` — check Resolved/Closed, 1 lần/report. `POST /reports/{id}/rate`    |
| BR-REP-019      | Draft max 3, xóa 7d                      |   ✅   | `SaveDraft/`, `GetMyDrafts/`, `DeleteDraft/` + `DraftCleanupJob` (daily 03:00)    |
| BR-REP-020/021  | State machine + role transitions         |   ✅   | `Report.cs` state machine methods                                                 |
| BR-REP-022      | Reject reason ≥ 20 chars                 |   ✅   | `RejectReport/` validator                                                         |
| BR-REP-030      | Duplicate Tier 1 (geo ≤50m + category + 24h) | ✅ | Inline trong `SubmitPollutionReportCommandHandler` — `GeoMath.HaversineMeters` + bbox |
| BR-REP-031      | LEO xác nhận / bác bỏ nghi ngờ trùng      |   ✅   | `ConfirmDuplicate/`, `DismissDuplicate/`, `GetDuplicateCandidates/`               |
| BR-REP-032      | Merge duplicate (+50% điểm, media + comments) | ✅ | `ConfirmDuplicate` merge media + comments; `DuplicateMergedPointsHandler` (+50% ReportVerified) |
| BR-REP-033      | Citizen flag ≥3 → LEO review              |   ✅   | `FlagReport/` + `DuplicateReviewNeeded` template + `SendFromTemplateAsync`        |

**Duplicate detection — chi tiết triển khai (branch `feature/duplicate-ai-compare-image`):**

| Tầng | Cơ chế | Files chính |
|------|--------|---------------|
| Tier 1 | Inline submit: ≤50m + cùng category + ≤24h → `MarkPossibleDuplicate("geo_time")` | `GeoMath.cs`, `SubmitPollutionReportCommandHandler` |
| Tier 2 | Hangfire `CompareDuplicateImagesJob` → Python `POST /api/v1/compare-images` (DINOv2) | `AiImageCompareService`, `EnqueueDuplicateCompareHandler` |
| LEO review | `GET duplicate-candidates`, `POST confirm-duplicate`, `POST dismiss-duplicate`, `POST flag` | `ReportsController`, docs `fe-leo-duplicate-detection-guide.md` |
| Migrations | `AddDuplicateDetectionFields`, `AddPenaltyPaymentSoftDelete` | Chưa apply DB dev (cần `dotnet ef database update`) |

---

## B.4 Map (`BR-MAP-001..012`) — ⚠️ 2/8 rules

| BR         | Status | Ghi chú                                                           |
| ---------- | :----: | ----------------------------------------------------------------- |
| BR-MAP-001 |   ⚠️   | Default location logic — FE concern nhưng API cần hỗ trợ          |
| BR-MAP-002 |   ⚠️   | Nearby 5km — cần verify query                                     |
| BR-MAP-003 |   ⚠️   | Filter — có `GetPublicMapReports/` nhưng cần verify filter params |
| BR-MAP-004 |   ❌   | GPS round 10m cho public                                          |
| BR-MAP-005 |   ❌   | Clustering — FE + API support                                     |
| BR-MAP-010 |   ❌   | Hotspot detection                                                 |
| BR-MAP-011 |   ❌   | Heatmap cho Officer                                               |
| BR-MAP-012 |   ❌   | Redis cache 10 phút                                               |

---

## B.5 Officer (`BR-OFF-001..022`) — ✅ 12/12 rules

| BR         | Status | Ghi chú                                                                                                              |
| ---------- | :----: | -------------------------------------------------------------------------------------------------------------------- |
| BR-OFF-001 |   ✅   | GPS → Ward → LEO routing                                                                                             |
| BR-OFF-002 |   ✅   | SLA xác minh 24h → `SlaBreachVerificationJob` + notification LEO/DEO                                                 |
| BR-OFF-003 |   ✅   | Chỉnh loại/mức độ khi verify                                                                                         |
| BR-OFF-004 |   ✅   | Conflict of interest — Verify + Reject + Escalate                                                                    |
| BR-OFF-005 |   ✅   | Triage dọn dẹp + xử phạt — flow đúng (AssignTeam + CreateInspectionReport)                                           |
| BR-OFF-010 |   ✅   | Priority score formula → `PriorityScoreRefreshJob` every 30'                                                         |
| BR-OFF-011 |   ✅   | Gán team (community hoặc company)                                                                                    |
| BR-OFF-012 |   ✅   | Reassign                                                                                                             |
| BR-OFF-013 |   ✅   | Giới hạn 6 task/team, cảnh báo tại 5 — `WorkloadLimitsOptions` + enforce trong AssignTeam/AssignCompanyTeam/Reassign |
| BR-OFF-020 |   ✅   | SLA xử lý theo severity → `SlaBreachResolutionJob` + notification LEO                                                |
| BR-OFF-021 |   ✅   | KPI Officer — `GetOfficerKpiQuery` (custom From/To + preset period)                                                  |
| BR-OFF-022 |   ✅   | Export CSV/Excel — `ExportReportsQuery` (scope: LEO/DEO/Admin, PII control)                                          |

---

## B.6 Cleanup (`BR-CLN-001..008`) — ✅ 8/8 rules

| BR         | Status | Ghi chú                                                                                             |
| ---------- | :----: | --------------------------------------------------------------------------------------------------- |
| BR-CLN-001 |   ✅   | Tiếp nhận CleanupTask                                                                               |
| BR-CLN-002 |   ✅   | Check-in ≤ 200m (PostGIS `ST_Distance`) — `CheckInCleanup/` handler                                 |
| BR-CLN-003 |   ✅   | Check-in to start task — Assigned → InProgress via `CheckIn()` entity method                        |
| BR-CLN-004 |   ✅   | Update ≥ 1/ngày — `UpdateCleanupProgress/` handler + `CleanupProgressSlaJob` (hourly, flag 24h/48h) |
| BR-CLN-005 |   ✅   | ≥ 2 ảnh after — enforce count ≥ 2 trong `ResolveReportHandler`. Không áp dụng kiểm tra góc chụp.    |
| BR-CLN-006 |   ✅   | Escalate lên LEO — `EscalateCleanup/` handler, InProgress → Escalated                               |
| BR-CLN-007 |   ✅   | `DeclineAssignment/` — 24h window (user đã đổi từ 2h)                                               |
| BR-CLN-008 |   ✅   | Company team — code verify BR-CMP-005 check via `CompanyCascadeService`                             |

---

## B.7 Inspection (`BR-INS-001..032`) — ✅ 14/14 rules

| BR         | Status | Ghi chú                                                                                              |
| ---------- | :----: | ---------------------------------------------------------------------------------------------------- |
| BR-INS-001 |   ✅   | `CreateInspectionReport/` — mọi loại ô nhiễm                                                         |
| BR-INS-002 |   ✅   | Scope check — `GetInspectionQueue` + `InspectionTeamAuthorization` filter by team                    |
| BR-INS-003 |   ✅   | Từ chối task 24h — `DeclineInspection/` handler (24h window, user đã đổi từ 2h)                      |
| BR-INS-004 |   ✅   | Check-in ≤ 200m — `CheckInInspection/` handler (PostGIS `ST_Distance`)                               |
| BR-INS-010 |   ✅   | `UpdateInspectionDetails/` + `ViolatingEntity` (Individual/Business, TaxCode/CCCD). `CreateViolatingEntity/`, `SearchViolatingEntities/`, `GetViolatingEntityById/` |
| BR-INS-011 |   ✅   | ViolationLevel enum + `PenaltyFramework` entity (Admin configurable, BR-ADM-008)                     |
| BR-INS-012 |   ✅   | `IssuePenalty/` — repeat offender auto-detect bằng ViolatingEntityId FK (fallback string-match)      |
| BR-INS-013 |   ✅   | `CloseNoViolation/`                                                                                  |
| BR-INS-020 |   ✅   | `RecordPayment/` + `PenaltyPayment` entity — partial payment, evidence (ảnh biên lai), audit trail. Chỉ hỗ trợ nộp trực tiếp tại phường/xã |
| BR-INS-021 |   ✅   | `MarkOverdue/`                                                                                       |
| BR-INS-022 |   ✅   | Repeat offender — `ViolatingEntityId` FK query (≥ 2 / 12 tháng), fallback `ViolatorIdentity` string  |
| BR-INS-030 |   ✅   | SLA Inspection — `SlaBreachInspectionJob` (every 30', flag breach)                                   |
| BR-INS-031 |   ✅   | Update tiến độ ≥ 1/ngày — `UpdateInspectionProgress/` handler                                        |
| BR-INS-032 |   ✅   | KPI Inspection Team — `GetInspectionTeamKpi/` query (penalty on-time %, paid on-time %, repeat, SLA) |

> **TODO (P3):** BR-INS-020 — Bổ sung `PaymentMethod` enum (Cash / BankTransfer) khi cần hỗ trợ thanh toán online. Hiện chỉ hỗ trợ nộp trực tiếp tại phường/xã (InPerson).

---

## B.8 Company (`BR-CMP-001..021`) — ✅ 14/14 rules

| BR         | Status | Ghi chú                                                                                                                                                                                 |
| ---------- | :----: | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| BR-CMP-001 |   ✅   | `CreateCompany/`                                                                                                                                                                        |
| BR-CMP-002 |   ✅   | `ResetCompanyManagerPassword/` (reset-password flow)                                                                                                                                    |
| BR-CMP-003 |   ✅   | ContractType enum, ContractEndDate nullable                                                                                                                                             |
| BR-CMP-004 |   ✅   | 5 status (PendingActivation/Active/Suspended/Expired/Terminated) + `SuspendCompany/`, `TerminateCompany/`, `ReactivateCompany/`                                                         |
| BR-CMP-005 |   ✅   | `IsActive` property trên entity                                                                                                                                                         |
| BR-CMP-006 |   ✅   | `RenewContract/` command + `ContractPeriod` entity (1-N lịch sử kỳ HĐ) + `GetContractHistory/` query. Auto-reactivate Expired→Active. Migration seed kỳ ban đầu cho existing companies. |
| BR-CMP-007 |   ✅   | `CompanyContractExpiryJob` — auto-expire Bidding hết hạn + cảnh báo 30/7/1 ngày trước khi hết hạn (daily 02:00 UTC)                                                                     |
| BR-CMP-010 |   ✅   | `CreateCompanyStaff/`                                                                                                                                                                   |
| BR-CMP-011 |   ✅   | `CreateCompanyTeam/`, `AssignCompanyTeam/`                                                                                                                                              |
| BR-CMP-012 |   ✅   | `GetCompanyServiceAreas/`, `UpdateCompanyServiceAreas/`                                                                                                                                 |
| BR-CMP-013 |   ✅   | `CompanyCascadeService`: Suspend/Terminate/Expire → auto-decline assignments (ForceDecline) + revert reports → Verified + notify LEO                                                    |
| BR-CMP-014 |   ✅   | N-N `CompanyServiceArea`                                                                                                                                                                |
| BR-CMP-020 |   ✅   | `GetCompanyKpi/` query — task volume (assigned/completed/declined), SLA compliance rate, avg resolution hours. DEO xem theo company, CM xem company mình. Reuse `KpiPeriod` enum.       |
| BR-CMP-021 |   ✅   | **Audit passed** — tất cả 11 CM/CompanyStaff handlers đều resolve `companyId` từ token via `CompanyStaff.GetByUserIdAsync()` + filter queries by companyId. Không gap nào.              |

---

## B.9–B.14 Modules bổ sung

### ✅ Notifications (`BR-NTF-001..004`) — ĐÃ IMPLEMENT

- BR-NTF-001 (Kênh): ✅ Push (FCM) + Email. User cấu hình bật/tắt per-type via `PUT v1/notifications/preferences`
- BR-NTF-002 (Events): ✅ ReportStatusChanged + NewComment (`CommentPostedNotificationHandler`) + DuplicateReviewNeeded (BR-REP-033)
- BR-NTF-003 (Anti-spam): ✅ Max 20/type/ngày. Digest cuối ngày chưa có (P2)
- BR-NTF-004 (i18n): ⚠️ Hardcode vi-VN. Resource files cho en-US chưa có (P2)

**Files mới:**

- `Notification.cs`, `NotificationPreference.cs` — Domain entities
- `NotificationType.cs`, `NotificationChannel.cs` — Enums
- `INotificationService.cs`, `IPushNotificationSender.cs` — Application interfaces
- `NotificationService.cs` — Orchestrate preference → anti-spam → persist → dispatch
- `FcmPushNotificationSender.cs` — Firebase Cloud Messaging
- `ReportStatusNotificationHandler.cs` — Domain event → notification (decoupled)
- 5 feature slices: GetMyNotifications, MarkNotificationRead, MarkAllRead, GetNotificationPreferences, UpdateNotificationPreferences, UpdateDeviceToken
- `NotificationsController.cs` — 6 API endpoints
- Migration: `202606280900_AddNotificationsAndSlaBreachFields`

### ✅ Comments (`BR-CMT-001..004`) — ĐÃ IMPLEMENT

| BR | Mô tả | Evidence |
|----|-------|----------|
| BR-CMT-001 | Auth + guard báo cáo `hideReporterName` | `CommentAccess.cs`, `AddCommentCommandHandler` |
| BR-CMT-002 | 1–500 ký tự, max 2 ảnh ≤5MB | Validators + `UploadCommentImage/` |
| BR-CMT-003 | Word filter (phase 1); 3 strike → ban 7 ngày | `ProfanityFilter`, `User.RecordCommentViolation()` — AI text deferred |
| BR-CMT-004 | Sửa/xóa 15 phút; LEO hide | `EditComment/`, `DeleteComment/`, `HideComment/` |

**API & docs:**

- `CommentsController` — 5 endpoints (`GET/POST /reports/{id}/comments`, `PUT/DELETE /comments/{id}`, `POST /comments/{id}/hide`)
- `POST /v1/media/comments/images` — upload ảnh đính kèm
- `fe-comments-api-guide.md`, link từ `fe-citizen-map-report-detail.md`
- BR-REP-032: merge comments trong `ConfirmDuplicateCommandHandler`
- Migration: `20260714184414_AddCommentModule`
- Tests: `CommentTests`, `CommentAccessTests`, `ProfanityFilterTests` (BR-CMT-*)

### ✅ Gamification (`BR-GAM-001..006`) — ĐÃ IMPLEMENT (branch `feature/gamification-module`)

- BR-GAM-001 (Points Formula): ✅ Verified+10, Resolved+20, Penalty+20, Duplicate merge +50% ReportVerified (BR-REP-032), Reject-5. Idempotent
- BR-GAM-002 (Anonymous opt-out): ⚠️ Entity sẵn sàng, nhưng chưa có cột `IsAnonymous` trên User → dùng khi có Privacy settings
- BR-GAM-003 (Levels L1–L5): ✅ Computed property: 0–99=L1, 100–499=L2, 500–1499=L3, 1500–4999=L4, ≥5000=L5. `LevelUpEvent` domain event
- BR-GAM-004 (Badges): ✅ Seed 4 badges: `first_report`, `eco_warrior`, `hotspot_hunter`, `streak_7d`
  - `first_report`: ✅ auto-award khi có ≥1 report verified
  - `eco_warrior`: ✅ auto-award khi ≥10 reports verified
  - `hotspot_hunter`: ⚠️ **seed nhưng chưa auto-award** — chờ BR-MAP-010 (hotspot detection)
  - `streak_7d`: ⚠️ **seed nhưng chưa auto-award** — cần consecutive-day tracking
  - `verified_citizen`: ❌ bỏ qua — chờ KYC module
    > **TODO:** Khi implement BR-MAP-010, enable auto-award cho `hotspot_hunter` trong `CheckBadgesCommandHandler`
- BR-GAM-005 (Leaderboard): ✅ GetLeaderboard query (Weekly/Monthly/Yearly). LeaderboardSnapshotJob (Hangfire, daily 00:05 UTC)
- BR-GAM-006 (Fraud Lock): ✅ LockGamification command (Admin only). Deduct all points + block 30 days

**Infrastructure thêm:**

- DomainEvent dispatch wired trong `UnitOfWork.SaveChanges` (MediatR `IPublisher`)
- Report.Verify/Reject/Resolve raise `ReportVerifiedEvent`/`ReportRejectedEvent`/`ReportResolvedEvent`
- Hangfire + PostgreSql storage (NuGet mới)
- Dashboard: `/hangfire`
- 4 API endpoints: `GET /v1/gamification/my-points`, `GET /v1/gamification/my-badges`, `GET /v1/gamification/leaderboard`, `POST /v1/gamification/{userId}/lock`
- 11 unit tests (UserPoints entity)

### ⚠️ AI Service (`BR-AI-001..007`) — 5/7 rules

| BR         | Mô tả                                      | Status | Evidence                                                                 |
| ---------- | ------------------------------------------ | :----: | ------------------------------------------------------------------------ |
| BR-AI-001  | Phân loại ảnh (3 loại v1.2)                |   ✅   | `AnalyzeReportImage/`, `AiClassificationService`                         |
| BR-AI-002  | Image similarity (duplicate Tier 2)        |   ✅   | `AiImageCompareService` → Python DINOv2 `/api/v1/compare-images`         |
| BR-AI-003  | Severity estimation                        |   ⚠️   | AI trả severity; mapper `AiSeverityMapper` — cần verify model output     |
| BR-AI-004  | Anti-fraud / irrelevant image              |   ✅   | `AnalyzeReportImage` decision `IRRELEVANT_OR_SUSPECTED_ABUSIVE`          |
| BR-AI-005  | Confidence threshold                       |   ⚠️   | Có `confidence` field; ngưỡng reject chưa document rõ trong handler      |
| BR-AI-006  | Fallback `ai_pending` retry trong 1h       |   ✅   | `AiRetryJob` (every 5', batch 50) + `AiPending` trên manual submit flow    |
| BR-AI-007  | Strip EXIF GPS trước khi gửi AI              |   ❌   | Chưa implement — ảnh gửi AI chưa strip EXIF nhạy cảm                      |

### ✅ Administration (`BR-ADM-001..012`) — 12/12 rules

| BR         | Status | Ghi chú / Evidence                                                                                                                         |
| ---------- | :----: | ------------------------------------------------------------------------------------------------------------------------------------------ |
| BR-ADM-001 |   ✅   | Admin quản lý user: `AdminController.CreateAccount/UpdateUser/DeleteUser` (soft-delete). Ghi audit log qua `AuditLogBehavior`.             |
| BR-ADM-002 |   ✅   | 8 roles hệ thống gán cho user qua `UserRole` enum. Admin đổi role qua `UpdateUserRoleCommand` (ghi audit log).                             |
| BR-ADM-003 |   ✅   | CRUD Category: `CreateCategory/UpdateCategory/ArchiveCategory`. Loại đang sử dụng chỉ cho phép 'Archive' (ẩn khi chọn mới).                |
| BR-ADM-004 |   ✅   | Template thông báo i18n: `NotificationTemplate` entity + CRUD + publish flow. Placeholder whitelist regex validation + test-send API.      |
| BR-ADM-005 |   ✅   | Gamification config: `GamificationConfig` entity, CRUD endpoints. Event handler `ReportPointsHandlers` đọc cấu hình trực tiếp từ DB.       |
| BR-ADM-006 |   ✅   | Content moderation: Admin ẩn/bỏ ẩn báo cáo vi phạm qua `HideReport/UnhideReport` commands. Public queries lọc bỏ báo cáo bị ẩn.            |
| BR-ADM-007 |   ✅   | Spam dashboard: `GetSpamSuspectsQuery` lọc danh sách tài khoản nghi spam theo heuristic (submit/giờ, reject/tuần, AI flag) trên DB.        |
| BR-ADM-008 |   ✅   | Khung tiền phạt: `PenaltyFramework` entity + CRUD. Unique index cho `(CategoryId, ViolationLevel)` active. MinAmount <= MaxAmount.         |
| BR-ADM-009 |   ✅   | Phân quyền dữ liệu theo phạm vi: DEO lọc theo tỉnh, LEO lọc theo xã/phường, Company lọc theo CM/Staff (ví dụ: `GetCompaniesQuery`).        |
| BR-ADM-010 |   ✅   | Hệ thống Audit log: `AuditLogBehavior` tự động ghi log các `IAuditable` commands nhạy cảm. `AuditLogRetentionJob` dọn dẹp log > 12 tháng.  |
| BR-ADM-011 |   ✅   | Sao lưu dữ liệu tự động định kỳ hàng ngày (Infra / DevOps concern).                                                                        |
| BR-ADM-012 |   ✅   | Giám sát công ty: Admin xem toàn bộ (mọi tỉnh); DEO chỉ xem & quản lý công ty có ServiceArea thuộc tỉnh mình (`GetCompaniesQueryHandler`). |

### ✅ Data Privacy (`BR-DAT-001..005`) — 5/5 rules

| BR         | Status | Ghi chú                                                                                                                                 |
| ---------- | :----: | --------------------------------------------------------------------------------------------------------------------------------------- |
| BR-DAT-001 |   ✅   | `BcryptPasswordHasher` 12 rounds ✅. TLS — infra (reverse proxy). Secrets qua env vars, không hardcode                                  |
| BR-DAT-002 |   ✅   | `DataRetentionJob` (weekly Sunday 04:00 UTC): xóa S3 files ảnh >2 năm (giữ DB record), hard-delete audit log >12 tháng                  |
| BR-DAT-003 |   ✅   | `ExportMyDataQuery` → `GET /v1/users/me/data-export`: export profile + reports + notifications + gamification. Hỗ trợ JSON + CSV        |
| BR-DAT-004 |   ✅   | Infra concern — pg_dump daily, 30 bản, S3 lifecycle. Không cần code backend                                                             |
| BR-DAT-005 |   ✅   | `User.HasDataConsent` + `ConsentAcceptedAt`. `POST /v1/users/me/consent` khi mở app lần đầu. SubmitReport handler chặn nếu chưa consent |

### ❌ Non-functional (`BR-SYS-001..006`)

- BR-SYS-004: Rate limiting ❌
- Còn lại: infra/DevOps concern

---

## B.15 Background Jobs — ✅ 13/13 recurring + 1 on-demand

| Job                            | BR             | Lịch / Trigger | Mô tả                                                                                      |
| ------------------------------ | -------------- | -------------- | ------------------------------------------------------------------------------------------ |
| `AutoCloseResolvedReportJob`   | BR-REP-016     | hourly         | ✅ Resolved → Closed sau 7 ngày (batch 100)                                                |
| `SlaBreachVerificationJob`     | BR-OFF-002     | every 15'      | ✅ Submitted > 24h → flag breached + notification                                          |
| `SlaBreachResolutionJob`       | BR-OFF-020     | every 30'      | ✅ InProgress > SLA → flag breached + notification                                           |
| `OverdueReportNotificationJob` | BR-REP-008/009 | hourly         | ✅ Pending > 72h, Verified > 24h                                                            |
| `PriorityScoreRefreshJob`      | BR-OFF-010     | every 30'      | ✅ Recalculate priority scores                                                             |
| `DraftCleanupJob`              | BR-REP-019     | daily 03:00    | ✅ Draft > 7 ngày → xóa                                                                      |
| `DataRetentionJob`             | BR-DAT-002     | weekly Sun 04:00 | ✅ Xóa ảnh S3 >2 năm, audit log >12 tháng                                                  |
| `AccountHardDeleteJob`         | BR-AUTH-021    | daily          | ✅ Soft delete > 90d → hard delete                                                           |
| `LeaderboardSnapshotJob`       | BR-GAM-005     | daily 00:05    | ✅ Leaderboard snapshot                                                                    |
| `CompanyContractExpiryJob`     | BR-CMP-007     | daily 02:00    | ✅ Bidding hết hạn → Expired + cảnh báo CM 30/7/1 ngày                                     |
| `SlaBreachInspectionJob`       | BR-INS-030     | every 30'      | ✅ InspectionReport > SLA → flag breach                                                      |
| `CleanupProgressSlaJob`        | BR-CLN-004     | hourly         | ✅ Assignment InProgress > 24h/48h → warn/flag stale                                         |
| `AiRetryJob`                   | BR-AI-006      | every 5'       | ✅ `ai_pending` retry trong 1h (batch 50)                                                   |
| `CompareDuplicateImagesJob`    | BR-REP-030/AI-002 | **on-demand** | ✅ Enqueue sau Tier 1 flag — không phải recurring cron                                      |

> [!NOTE]
> Hangfire: DI + PostgreSql storage + Dashboard `/hangfire`.
> `TransactionBehavior` wrap mọi Command trong DB transaction.
> **13 recurring jobs** đã registered; `CompareDuplicateImagesJob` enqueue qua `DuplicateCompareScheduler`.

---

## B.16 Domain Entity Gaps

| Entity                                   | Có                             | Thiếu                                                           |
| ---------------------------------------- | ------------------------------ | --------------------------------------------------------------- |
| `CompanyStatus` enum                     | ✅ 5 values (incl. Terminated) | Suspend/Terminate/Reactivate/Expire transitions implemented     |
| `PollutionCategory`                      | ✅ Configurable                | ⚠️ Seed data cần đúng 3 loại (v1.2 bỏ Không khí, Tiếng ồn)      |
| `Invitation` entity                      | ✅ ĐÃ IMPLEMENT                | BR-ORG-020/021: `StaffInvitation` entity, 7d expiry, single-use |
| `Comment` entity                         | ✅                             | BR-CMT-001..004, BR-REP-032 merge                               |
| `Badge`, `UserPoints`                    | ✅ ĐÃ IMPLEMENT                | BR-GAM-001..006                                                 |
| `Notification`, `NotificationPreference` | ✅ ĐÃ IMPLEMENT                | BR-NTF-001..004                                                 |
| `ReportDraft`                            | ✅                             | Max 3 + `DraftCleanupJob` đã có                                     |

---

# Phần C — Đề xuất thứ tự ưu tiên

## P0 — Blocking ✅ HOÀN THÀNH

| #   | Task                                                             | BRs                    | Status |
| --- | ---------------------------------------------------------------- | ---------------------- | :----: |
| 1   | **Hangfire setup** + `AutoCloseResolvedReportJob`                | BR-REP-016             |   ✅   |
| 2   | **`CompanyStatus.Terminated`** thêm vào enum + entity transition | BR-CMP-004             |   ✅   |
| 3   | **`TransactionBehavior`** (MediatR pipeline)                     | Mọi Command            |   ✅   |
| 4   | **SLA jobs** (Verification 24h + Resolution theo severity)       | BR-OFF-002, BR-OFF-020 |   ✅   |

## P1 — Core Business Value

| #   | Task                                                     | BRs                        | Status |
| --- | -------------------------------------------------------- | -------------------------- | :----: |
| 5   | **Notifications** (FCM + Email templates + preferences)  | BR-NTF-001..004            |   ✅   |
| 6   | **Company lifecycle** (Suspend/Terminate/Reactivate)     | BR-CMP-004, BR-CMP-013     |   ✅   |
| 7   | **Company contract expiry job** (auto-expire + warnings) | BR-CMP-007                 |   ✅   |
| 8   | **Workload limit** (6 task/team, warn at 5)              | BR-OFF-013                 |   ✅   |
| 9   | **Comments** (entity + CRUD + moderation)                | BR-CMT-001..004            |   ✅   |
| 10  | **Brute-force protection** (sliding window + Turnstile)  | BR-AUTH-014                |        |
| 11  | **Rate limiting** (Redis + ASP.NET middleware)           | BR-SYS-004, BR-REP-010     | ⚠️ BR-REP-010 submit quota ✅; BR-SYS-004 global middleware ❌ |
| 12  | ~~**Check-in ≤ 200m** (PostGIS ST_DWithin)~~             | BR-CLN-002/003, BR-INS-004 |   ✅   |
| 13  | ~~**Company contract renewal** (gia hạn/tái ký)~~        | BR-CMP-006                 |   ✅   |
| 14  | ~~**Duplicate detection** (geo Tier 1 + AI Tier 2)~~     | BR-REP-030..033            |   ✅   |

## P2 — Enhancement

| #   | Task                                           | BRs                                    | Status |
| --- | ---------------------------------------------- | -------------------------------------- | :----: |
| 12  | ~~**Gamification** (points, badges, leaderboard)~~ | BR-GAM-001..006                    |   ✅   |
| 13  | ~~**Duplicate detection** (geo + AI DINOv2)~~  | BR-REP-030..033                        |   ✅   |
| 14  | ~~**Priority score formula**~~                 | BR-OFF-010                             |   ✅   |
| 15  | ~~**KPI / Analytics / Export**~~               | BR-OFF-021/022, BR-CMP-020, BR-INS-032 |   ✅   |
| 16  | **Content moderation** (word filter report desc + AI text) | BR-REP-004, BR-CMT-003 (AI) | ✅ word filter comment + report desc |
| 17  | ~~**Invitation entity** (7d expiry, single-use)~~ | BR-ORG-021                          |   ✅   |
| 18  | **Map enhancements** (hotspot, cache, heatmap) | BR-MAP-004..012                        |        |
| 19  | **EXIF strip** trước AI                        | BR-AI-007, BR-REP-011                  |        |

## P3 — Hardening & Compliance

| #   | Task                                            | BRs             | Status |
| --- | ----------------------------------------------- | --------------- | :----: |
| 20  | **Integration tests** (Testcontainers Postgres) | Testing pyramid |        |
| 21  | **Security headers** (OwaspHeaders.Core)        | BR-DAT-001      |        |
| 22  | ~~**Audit log** (comprehensive)~~               | BR-ADM-010      |   ✅   |
| 23  | ~~**Data privacy** (consent, export, retention)~~ | BR-DAT-002..005 |   ✅   |
| 24  | **EXIF validation**                             | BR-REP-011      | ✅     |

---

# Phần D — Đề xuất cập nhật OVERVIEW.md

> [!IMPORTANT]
> Phê duyệt để tôi tiến hành cập nhật OVERVIEW.md với các nội dung ở Phần A.

Cụ thể tôi sẽ:

1. **§5 "Một số rule cần chú ý đặc biệt"** — bổ sung các BR ID thiếu (BR-ORG-020/021, BR-CMP-006/010/011/013, BR-INS-003/004/030..032, BR-ADM-012)
2. **Sửa "v1.3" → "v1.2"** (line 21, 549, 588) — tài liệu BR chỉ có v1.2
3. **Bổ sung `CompanyStatus.Terminated`** mention vào §5 mapping
4. **Bổ sung note "3 loại ô nhiễm"** (đã ghi chưa đủ rõ ở §1)
5. **Bổ sung mục BR-CMP-013** (company suspend → task reassignment) vào §5 "rule cần chú ý"
6. **Bổ sung BR-INS-022** (repeat offender) vào §5
7. **Sửa BR-AUTH-022 → BR-AUTH-021** theo đánh số v1.2

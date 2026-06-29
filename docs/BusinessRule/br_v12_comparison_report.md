# Báo cáo: BR v1.2 ↔ OVERVIEW.md ↔ Codebase

> **Source of truth:** [SU26SE049_BusinessRules_v1_2.md](file:///d:/LEARNING/S9SU26/SEP490/greenlens-service/SU26SE049_BusinessRules_v1_2.md) (v1.2, 07/06/2026)
> **OVERVIEW hiện tại:** [OVERVIEW.md](file:///d:/LEARNING/S9SU26/SEP490/greenlens-service/OVERVIEW.md) v1.5

---

# Phần A — BR v1.2 vs OVERVIEW.md (Những gì OVERVIEW thiếu/sai)

OVERVIEW.md v1.5 tuyên bố đã "Đồng bộ với SU26SE049_BusinessRules_v1_2.docx". Tuy nhiên, nhiều BR ID mới trong v1.2 **KHÔNG được reference** trong OVERVIEW:

## A.1 BR IDs thiếu hoàn toàn trong OVERVIEW §5

| BR ID | Nội dung | Ưu tiên bổ sung |
|---|---|:---:|
| **BR-ORG-005** | Mô hình đa đơn vị trong 1 phường | **Cao** — OVERVIEW nêu ý tưởng (§1.1F) nhưng không tag BR ID |
| **BR-ORG-020** | Mời thành viên đội cộng đồng (LEO mời qua email) | **Cao** — flow mới v1.2 |
| **BR-ORG-021** | Hiệu lực lời mời (7 ngày, single-use) | **Cao** |
| **BR-OFF-005** | Triage: dọn dẹp & xử phạt song song | Có nhắc (line 588) nhưng ghi **"v1.3"** thay vì **"v1.2"** |
| **BR-CLN-008** | Đội Company: CM phân công, kiểm tra hiệu lực hợp đồng | **Trung bình** |
| **BR-CMP-006** | Gia hạn / tái ký hợp đồng | **Trung bình** |
| **BR-CMP-010** | CM thêm nhân viên (mời email) | **Cao** |
| **BR-CMP-011** | CM lập & điều phối đội | **Cao** — đã có code nhưng OVERVIEW không tag |
| **BR-CMP-012** | Phạm vi phục vụ (N-N check) | Có gián tiếp qua BR-CMP-014 |
| **BR-CMP-013** | Vô hiệu hóa kế thừa (suspend → staff mất quyền) | **Cao** — critical business rule |
| **BR-CMP-020** | KPI công ty | **Trung bình** |
| **BR-CMP-021** | Phân tách dữ liệu công ty | **Trung bình** |
| **BR-INS-002** | Chỉ xem task được gán (scope check) | **Thấp** — implicit |
| **BR-INS-003** | Từ chối task 2h | **Trung bình** |
| **BR-INS-004** | Check-in ≤ 200m | **Trung bình** |
| **BR-INS-030** | SLA Inspection = SLA Cleanup | **Cao** — SLA chưa implement |
| **BR-INS-031** | Update tiến độ ≥ 1/ngày | **Trung bình** |
| **BR-INS-032** | KPI Inspection Team | **Trung bình** |
| **BR-ADM-012** | Giám sát công ty (Admin toàn quốc, DEO theo tỉnh) | **Trung bình** |

## A.2 Sai lệch cần sửa

| Vị trí OVERVIEW | Sai | Đúng (BR v1.2) |
|---|---|---|
| Line 21, 549 | Ghi **"v1.3"** | Phải là **"v1.2"** — tài liệu BR chỉ có v1.2 |
| Line 581 | `BR-AUTH-022` (xóa tài khoản) | BR v1.2 đánh số là **`BR-AUTH-021`** |
| Line 33 (CompanyStatus enum) | Có 4 status: `PendingActivation, Active, Suspended, Expired` | BR-CMP-004 yêu cầu **5 status**: thêm **`Terminated`** |
| §5 BR mapping table | Không mention BR-REP-005 giảm từ 5 → 3 loại | BR v1.2 chỉ còn **3 loại** (Rác thải, Nước thải, Hóa chất) — bỏ Không khí, Tiếng ồn |

## A.3 Thiếu ý quan trọng trong OVERVIEW

| Ý trong BR v1.2 | Mô tả | Section cần bổ sung |
|---|---|---|
| **BR-ORG-016** | Escalation tuyến cấp TP (cờ admin cấu hình) | §5 State Machine — đã nhắc nhưng chưa đủ chi tiết về cơ chế cờ |
| **BR-REP-018** | Citizen đánh giá sau Resolved | Chưa mention |
| **BR-CMP-013** | Khi Suspend/Terminate company → push tasks về LEO | Chưa mention — critical cho task reassignment |
| **BR-INS-022** | Tái phạm (≥ 2 biên bản / 12 tháng → nâng mức phạt) | Chưa mention — business logic phức tạp |
| **BR-REP-033** | Flag duplicate bởi người dùng (≥ 3 flag → LEO xem xét) | Chưa mention |

---

# Phần B — BR v1.2 vs Codebase (Đã làm / Chưa làm)

## Tổng quan nhanh

| Trạng thái | Modules | % ước tính |
|:---:|:---|:---:|
| ✅ Core hoàn thành | Auth, Reports (32 slices), Organization (39 slices), Inspection (10 slices), Map (2), Catalog (3), Admin (10), Media, Users | ~60% |
| ⚠️ Implement một phần | Company (thiếu Terminated + contract renewal), Cleanup (thiếu check-in/SLA), Domain (thiếu CompanyStatus.Terminated) | ~15% |
| ❌ Chưa implement | Notifications, Comments, Gamification, Background Jobs, Rate Limiting, Brute-force, Analytics/KPI | ~25% |

---

## B.1 Auth & Account (`BR-AUTH-001..021`) — ✅ 15/17 rules

| BR | Mô tả | Status | Evidence |
|---|---|:---:|---|
| BR-AUTH-001 | Email RFC 5322 | ✅ | `Register/` validator |
| BR-AUTH-002 | Email unique | ✅ | `Register/` handler |
| BR-AUTH-003 | SĐT VN format | ✅ | `Register/` validator |
| BR-AUTH-004 | SĐT unique | ✅ | `Register/` handler |
| BR-AUTH-005 | Password strength | ✅ | `Register/` validator |
| BR-AUTH-006 | Confirm password | ✅ | `Register/` validator |
| BR-AUTH-007 | OTP 10 phút | ✅ | `RequestOtp/`, `VerifyOtp/` |
| BR-AUTH-008 | Default role Citizen | ✅ | `Register/` handler |
| BR-AUTH-009 | Phân cấp quyền tạo role (Admin/DEO/LEO/CM) | ⚠️ | `UpdateUserRole/` có, nhưng phân cấp DEO→CM chưa rõ |
| BR-AUTH-010 | Required fields | ✅ | `Register/` validator |
| BR-AUTH-011 | Tên 2-50 ký tự tiếng Việt | ⚠️ | Cần verify regex |
| BR-AUTH-012 | Accept Terms | ❌ | Chưa thấy consent field |
| BR-AUTH-013 | Login email/SĐT | ✅ | `Login/` feature |
| BR-AUTH-014 | Brute-force lock 30' + CAPTCHA lần 3 | ❌ | Chưa có sliding window + Turnstile |
| BR-AUTH-015 | Block Inactive/Banned + Expired company | ⚠️ | Login handler cần verify company status check |
| BR-AUTH-016 | JWT 24h + Refresh 30d | ✅ | `RefreshToken/`, `JwtService.cs` |
| BR-AUTH-017 | Guest read-only (bỏ anonymous submit) | ✅ | `[Authorize]` trên submit endpoints |
| BR-AUTH-018 | Forgot/Reset password | ✅ | `ForgotPassword/`, `ResetPassword/` |
| BR-AUTH-019 | Update profile | ✅ | `Users/UpdateUserProfile/` |
| BR-AUTH-020 | Change password (không trùng 3 MK cũ) | ⚠️ | `ChangePassword/` có, chưa rõ history check |
| BR-AUTH-021 | Xóa tài khoản soft delete 90d | ⚠️ | `DeleteUser/` + `SoftDeletableEntity` có, chưa có `AccountHardDeleteJob` |

---

## B.2 Organization & Routing (`BR-ORG-001..021`) — ✅ 8/12 rules

| BR | Mô tả | Status | Evidence |
|---|---|:---:|---|
| BR-ORG-001 | Department cấp Tỉnh | ✅ | `CreateDepartment/`, `GetDepartments/` |
| BR-ORG-002 | Local Office cấp Xã/Phường | ✅ | `CreateLocalOffice/`, CRUD |
| BR-ORG-003 | Office → Team (1:N) | ✅ | `CreateTeam/`, `AddTeamMember/`, `RemoveTeamMember/` |
| BR-ORG-004 | Ward polygon geo routing | ✅ | `Ward.cs`, PostGIS |
| BR-ORG-005 | Đa đơn vị trong 1 phường | ✅ | N-N model `CompanyServiceArea` |
| BR-ORG-010 | GPS → Ward → LEO routing | ✅ | `SubmitPollutionReport/` handler |
| BR-ORG-011 | Department Common Queue | ❌ | Chưa có feature rõ ràng |
| BR-ORG-012 | Conflict of interest (LEO ≠ reporter) | ⚠️ | Cần verify trong handler |
| BR-ORG-013 | Quyết định xử lý khi xác minh (dọn dẹp + xử phạt song song) | ⚠️ | Có `VerifyReport/` + `CreateInspectionReport/` nhưng chưa enforce "ít nhất 1 nhánh" |
| BR-ORG-014 | SLA tiếp nhận 24h → escalate DEO | ❌ | Chưa có job |
| BR-ORG-015 | Re-assign khi LEO reject | ⚠️ | Có `RejectReport/` nhưng chưa đẩy về queue |
| BR-ORG-016 | Escalation tuyến cấp TP (cờ) | ❌ | Chưa có cờ "tuyến cấp TP" |
| BR-ORG-020 | Mời thành viên đội (LEO mời qua email) | ✅ | `RecruitStaff/`, `LookupCitizenByEmail/` |
| BR-ORG-021 | Hiệu lực lời mời 7 ngày | ❌ | Chưa có invitation entity/expiry |

---

## B.3 Report (`BR-REP-001..033`) — ✅ 15/23 rules

| BR | Mô tả | Status | Evidence |
|---|---|:---:|---|
| BR-REP-001 | Ảnh 1-5, ≤ 10MB | ✅ | Validator + `UploadReportImage/` |
| BR-REP-002 | Video 1, mp4/mov | ✅ | `UploadReportVideo/` + `FFmpegVideoTranscoder` (H.264 720p CRF 28, max 100MB/60s) |
| BR-REP-003 | GPS Vietnam bounds | ✅ | Validator |
| BR-REP-004 | Mô tả: filter tục tĩu | ❌ | Chưa có word filter |
| BR-REP-005 | **3 loại** ô nhiễm (Rác, Nước, Hóa chất) | ⚠️ | `PollutionCategory` là configurable, cần seed đúng 3 loại |
| BR-REP-006 | Severity 4 mức | ✅ | `Severity.cs` enum |
| BR-REP-008 | Cảnh báo tồn đọng 72h | ❌ | Chưa có job |
| BR-REP-009 | Cảnh báo chưa phân công 24h | ❌ | Chưa có job |
| BR-REP-010 | Rate limit 5/h, 20/24h | ❌ | Chưa có Redis sorted set |
| BR-REP-011 | EXIF metadata validation | ❌ | Chưa có |
| BR-REP-012 | Ẩn danh tính (tùy chọn) | ⚠️ | Cần verify field trên Report |
| BR-REP-013 | Initial status = Submitted | ✅ | `Report.cs` factory |
| BR-REP-014 | Ảnh before/after khi Resolved | ⚠️ | `UploadProgressImage/` có, chưa enforce ≥ 1 before + ≥ 1 after |
| BR-REP-015 | Citizen xác nhận (7d, max 2 re-open) | ⚠️ | `CloseReport/` + `ReopenReport/` có, max 2 cần verify |
| BR-REP-016 | Auto-close 7 ngày | ❌ | `AutoCloseResolvedReportJob` chưa có |
| BR-REP-017 | Không xóa report đã verified | ⚠️ | Cần verify |
| BR-REP-018 | Đánh giá của Citizen sau Resolved | ❌ | Chưa có feature |
| BR-REP-019 | Draft max 3, xóa 7d | ⚠️ | `ReportDraft.cs` tồn tại, chưa có `DraftCleanupJob` |
| BR-REP-020/021 | State machine + role transitions | ✅ | `Report.cs` state machine methods |
| BR-REP-022 | Reject reason ≥ 20 chars | ✅ | `RejectReport/` validator |
| BR-REP-030..033 | Duplicate detection | ❌ | Chưa implement |

---

## B.4 Map (`BR-MAP-001..012`) — ⚠️ 2/8 rules

| BR | Status | Ghi chú |
|---|:---:|---|
| BR-MAP-001 | ⚠️ | Default location logic — FE concern nhưng API cần hỗ trợ |
| BR-MAP-002 | ⚠️ | Nearby 5km — cần verify query |
| BR-MAP-003 | ⚠️ | Filter — có `GetPublicMapReports/` nhưng cần verify filter params |
| BR-MAP-004 | ❌ | GPS round 10m cho public |
| BR-MAP-005 | ❌ | Clustering — FE + API support |
| BR-MAP-010 | ❌ | Hotspot detection |
| BR-MAP-011 | ❌ | Heatmap cho Officer |
| BR-MAP-012 | ❌ | Redis cache 10 phút |

---

## B.5 Officer (`BR-OFF-001..022`) — ⚠️ 5/12 rules

| BR | Status | Ghi chú |
|---|:---:|---|
| BR-OFF-001 | ✅ | GPS → Ward → LEO routing |
| BR-OFF-002 | ❌ | SLA xác minh 24h → `SlaBreachVerificationJob` |
| BR-OFF-003 | ✅ | Chỉnh loại/mức độ khi verify |
| BR-OFF-004 | ⚠️ | Conflict of interest — cần verify |
| BR-OFF-005 | ⚠️ | Triage dọn dẹp + xử phạt — có pieces nhưng chưa enforce |
| BR-OFF-010 | ❌ | Priority score formula |
| BR-OFF-011 | ✅ | Gán team (community hoặc company) |
| BR-OFF-012 | ✅ | Reassign |
| BR-OFF-013 | ❌ | Giới hạn 10 task/team |
| BR-OFF-020 | ❌ | SLA xử lý theo severity |
| BR-OFF-021 | ❌ | KPI Officer |
| BR-OFF-022 | ❌ | Export CSV/Excel |

---

## B.6 Cleanup (`BR-CLN-001..008`) — ⚠️ 3/8 rules

| BR | Status | Ghi chú |
|---|:---:|---|
| BR-CLN-001 | ✅ | Tiếp nhận CleanupTask |
| BR-CLN-002 | ❌ | Check-in ≤ 200m (PostGIS) |
| BR-CLN-003 | ❌ | Check-in to start task |
| BR-CLN-004 | ❌ | Update ≥ 1/ngày, cảnh báo 24h/48h |
| BR-CLN-005 | ❌ | ≥ 2 ảnh after khác hash |
| BR-CLN-006 | ❌ | Escalate lên LEO |
| BR-CLN-007 | ⚠️ | `DeclineAssignment/` có, cần verify 2h window |
| BR-CLN-008 | ⚠️ | Company team — code có nhưng chưa verify BR-CMP-005 check |

---

## B.7 Inspection (`BR-INS-001..032`) — ✅ 8/14 rules

| BR | Status | Ghi chú |
|---|:---:|---|
| BR-INS-001 | ✅ | `CreateInspectionReport/` — mọi loại ô nhiễm |
| BR-INS-002 | ⚠️ | Scope check — cần verify |
| BR-INS-003 | ❌ | Từ chối task 2h |
| BR-INS-004 | ❌ | Check-in ≤ 200m |
| BR-INS-010 | ✅ | `UpdateInspectionDetails/` — biên bản |
| BR-INS-011 | ⚠️ | ViolationLevel enum có, khung phạt configurable chưa rõ |
| BR-INS-012 | ✅ | `IssuePenalty/` |
| BR-INS-013 | ✅ | `CloseNoViolation/` |
| BR-INS-020 | ✅ | `RecordPayment/` |
| BR-INS-021 | ✅ | `MarkOverdue/` |
| BR-INS-022 | ❌ | Repeat offender (≥ 2 biên bản / 12 tháng) |
| BR-INS-030 | ❌ | SLA Inspection |
| BR-INS-031 | ❌ | Update tiến độ ≥ 1/ngày |
| BR-INS-032 | ❌ | KPI Inspection Team |

---

## B.8 Company (`BR-CMP-001..021`) — ⚠️ 8/14 rules

| BR | Status | Ghi chú |
|---|:---:|---|
| BR-CMP-001 | ✅ | `CreateCompany/` |
| BR-CMP-002 | ✅ | `ResetCompanyManagerPassword/` (reset-password flow) |
| BR-CMP-003 | ✅ | ContractType enum, ContractEndDate nullable |
| BR-CMP-004 | ⚠️ | `CompanyStatus` enum thiếu **`Terminated`** (chỉ có 4/5 status) |
| BR-CMP-005 | ✅ | `IsActive` property trên entity |
| BR-CMP-006 | ❌ | Gia hạn / tái ký — chưa có feature |
| BR-CMP-007 | ❌ | `CompanyContractExpiryJob` chưa có |
| BR-CMP-010 | ✅ | `CreateCompanyStaff/` |
| BR-CMP-011 | ✅ | `CreateCompanyTeam/`, `AssignCompanyTeam/` |
| BR-CMP-012 | ✅ | `GetCompanyServiceAreas/`, `UpdateCompanyServiceAreas/` |
| BR-CMP-013 | ❌ | Suspend/Terminate → push tasks về LEO — chưa implement |
| BR-CMP-014 | ✅ | N-N `CompanyServiceArea` |
| BR-CMP-020 | ❌ | KPI công ty |
| BR-CMP-021 | ⚠️ | Phân tách dữ liệu — cần verify query filters |

---

## B.9–B.14 Chưa implement hoàn toàn

### ✅ Notifications (`BR-NTF-001..004`) — ĐÃ IMPLEMENT

- BR-NTF-001 (Kênh): ✅ Push (FCM) + Email. User cấu hình bật/tắt per-type via `PUT v1/notifications/preferences`
- BR-NTF-002 (Events): ✅ ReportStatusChanged (Verified/Rejected/Resolved → notify reporter)
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

### ❌ Comments (`BR-CMT-001..004`)
Chưa có feature module. Thiếu entity Comment.

### ✅ Gamification (`BR-GAM-001..006`) — ĐÃ IMPLEMENT (branch `feature/gamification-module`)
- BR-GAM-001 (Points Formula): ✅ Verified+10, Resolved+20, Penalty+20, Duplicate+5, Reject-5. Idempotent (same Report+Reason = skip)
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

### ❌ AI Service — BR v1.2 nói 3 loại (`BR-AI-001..007`)
- `AnalyzeReportImage/` ✅ + `AiClassificationService.cs` ✅
- BR-AI-001 cập nhật: AI phân loại **3 loại** (thay vì 5) → cần verify AI service config
- BR-AI-006: Fallback ai_pending ❌ — chưa có retry job

### ⚠️ Administration (`BR-ADM-001..012`)
- 10 admin features đã có (CRUD category, waste tags, force update, roles)
- **Thiếu:** BR-ADM-004 (notification templates), BR-ADM-005 (gamification config), BR-ADM-006 (content moderation), BR-ADM-007 (spam dashboard), BR-ADM-008 (khung tiền phạt configurable), BR-ADM-010 (audit log đầy đủ), BR-ADM-012 (giám sát công ty)

### ❌ Data Privacy (`BR-DAT-001..005`)
- BR-DAT-001: bcrypt ✅, TLS — infra
- BR-DAT-002..005: Retention policy, export data, backup, consent log → ❌

### ❌ Non-functional (`BR-SYS-001..006`)
- BR-SYS-004: Rate limiting ❌
- Còn lại: infra/DevOps concern

---

## B.15 Background Jobs — ❌ Chưa có

| Job | BR | Mô tả |
|---|---|---|
| `AutoCloseResolvedReportJob` | BR-REP-016 | ✅ Resolved → Closed sau 7 ngày (hourly, batch 100) |
| `SlaBreachVerificationJob` | BR-OFF-002 | ✅ Submitted > 24h → flag breached (every 15') |
| `SlaBreachResolutionJob` | BR-OFF-020 | ✅ InProgress > SLA → flag breached (every 30') |
| `OverdueReportNotificationJob` | BR-REP-008/009 | ❌ Pending > 72h, Verified > 24h |
| `AiRetryJob` | BR-AI-006 | ❌ ai_pending retry trong 1h |
| `DraftCleanupJob` | BR-REP-019 | ❌ Draft > 7 ngày → xóa |
| `CompanyContractExpiryJob` | BR-CMP-007 | ❌ Bidding hết hạn → Expired |
| `LeaderboardSnapshotJob` | BR-GAM-005 | ✅ Daily snapshot 00:05 UTC |
| `AuditLogRetentionJob` | BR-ADM-010 | ❌ Xóa log > 12 tháng |
| `AccountHardDeleteJob` | BR-AUTH-021 | ❌ Soft delete > 90d → hard delete |

> [!NOTE]
> Hangfire đã setup đầy đủ: DI, PostgreSql storage, Dashboard `/hangfire`.
> `TransactionBehavior` (MediatR pipeline) đã thêm — wrap mọi Command trong DB transaction.
> `LeaderboardSnapshotJob` + `AutoCloseResolvedReportJob` + `SlaBreachVerificationJob` + `SlaBreachResolutionJob` đã đăng ký.

---

## B.16 Domain Entity Gaps

| Entity | Có | Thiếu |
|---|---|---|
| `CompanyStatus` enum | ✅ 5 values (incl. Terminated) | — |
| `PollutionCategory` | ✅ Configurable | ⚠️ Seed data cần đúng 3 loại (v1.2 bỏ Không khí, Tiếng ồn) |
| `Invitation` entity | ❌ | BR-ORG-021: token, expiry 7d, single-use |
| `Comment` entity | ❌ | BR-CMT-001..004 |
| `Badge`, `UserPoints` | ✅ ĐÃ IMPLEMENT | BR-GAM-001..006 |
| `Notification`, `NotificationPreference` | ✅ ĐÃ IMPLEMENT | BR-NTF-001..004 |
| `ReportDraft` | ✅ | Thiếu max 3 check + cleanup job |

---

# Phần C — Đề xuất thứ tự ưu tiên

## P0 — Blocking ✅ HOÀN THÀNH

| # | Task | BRs | Status |
|---|---|---|:---:|
| 1 | **Hangfire setup** + `AutoCloseResolvedReportJob` | BR-REP-016 | ✅ |
| 2 | **`CompanyStatus.Terminated`** thêm vào enum + entity transition | BR-CMP-004 | ✅ |
| 3 | **`TransactionBehavior`** (MediatR pipeline) | Mọi Command | ✅ |
| 4 | **SLA jobs** (Verification 24h + Resolution theo severity) | BR-OFF-002, BR-OFF-020 | ✅ |

## P1 — Core Business Value

| # | Task | BRs | Status |
|---|---|---|:---:|
| 5 | **Notifications** (FCM + Email templates + preferences) | BR-NTF-001..004 | ✅ |
| 6 | **Comments** (entity + CRUD + moderation) | BR-CMT-001..004 |
| 7 | **Brute-force protection** (sliding window + Turnstile) | BR-AUTH-014 |
| 8 | **Rate limiting** (Redis + ASP.NET middleware) | BR-SYS-004, BR-REP-010 |
| 9 | **Check-in ≤ 200m** (PostGIS ST_DWithin) | BR-CLN-002/003, BR-INS-004 |
| 10 | **Company contract renewal** (gia hạn/tái ký) | BR-CMP-006 |
| 11 | **Company suspend → push tasks** | BR-CMP-013 |

## P2 — Enhancement

| # | Task | BRs |
|---|---|---|
| 12 | **Gamification** (points, badges, leaderboard) | BR-GAM-001..006 |
| 13 | **Duplicate detection** (GPS + time + pHash) | BR-REP-030..033 |
| 14 | **Priority score formula** | BR-OFF-010 |
| 15 | **KPI / Analytics / Export** | BR-OFF-021/022, BR-CMP-020, BR-INS-032 |
| 16 | **Content moderation** (word filter + AI) | BR-REP-004, BR-CMT-003 |
| 17 | **Invitation entity** (7d expiry, single-use) | BR-ORG-021 |

## P3 — Hardening & Compliance

| # | Task | BRs |
|---|---|---|
| 18 | **Integration tests** (Testcontainers Postgres) | Testing pyramid |
| 19 | **Security headers** (OwaspHeaders.Core) | BR-DAT-001 |
| 20 | **Audit log** (comprehensive) | BR-ADM-010 |
| 21 | **Data privacy** (consent, export, retention) | BR-DAT-002..005 |
| 22 | **EXIF validation** | BR-REP-011 |

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

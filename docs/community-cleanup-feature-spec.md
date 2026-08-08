# Community Cleanup — Full Feature Spec (GreenLens)

> **Ngày:** 2026-07-26  
> **Trạng thái:** Spec khóa để implement (chưa có trong BR v1.2 chính thức)  
> **Module đề xuất:** Community Cleanup (`BR-CMU-*` tạm — cần chủ BR phê duyệt ID chính thức)  
> **Liên quan hiện có:** BR-REP-020/021 (state machine), BR-CLN-002..005 (check-in / progress / after photos), BR-NTF-001..003, BR-OFF-011 (assign team — **bị thay thế** bởi luồng này khi active)

---

## 0. Tóm tắt 30 giây

LEO (web) chọn 1 báo cáo Verified → mở **chương trình dọn cộng đồng** → chỉ định **Leader** (1 Cleaner thuộc team bất kỳ) → Citizen trên mobile **Join (= Vote)** để vào làm → Leader cập nhật tiến độ / ảnh → Leader submit xác thực → **LEO bấm duyệt** → Report Resolved.

Đây là **luồng thay** `Assign Team` / Company dispatch trên cùng report (không chạy song song).

---

## 1. Quyết định đã khóa (product)

| # | Câu hỏi | Quyết định |
|---|---------|------------|
| 1 | Leader là ai? | **1 Cleaner** được LEO chỉ định, thuộc **một Cleanup team bất kỳ** |
| 2 | Join có duyệt không? | **Join = vào làm ngay** (không PendingApproval mặc định) |
| 3 | Vote nghĩa là gì? | **Vote = Join** (một hành động) |
| 4 | Quan hệ Assign Team? | **Thay thế** assign team / company trên report đó |
| 5 | Ai đóng report? | **Leader submit evidence** + **LEO Approve** → Resolved |
| 6 | Client nào? | **Mobile:** Citizen + Leader · **Web:** LEO tạo chương trình + assign Leader + verify |

---

## 2. Actors & quyền chi tiết

### 2.1 LEO (Web Dashboard)

| Được | Không |
|------|--------|
| Tạo / sửa lịch / đóng đăng ký / hủy chương trình | Check-in hiện trường trên mobile (không bắt buộc) |
| Chọn Leader = Cleaner trong team | Tự cập nhật % tiến độ thay Leader (P1 override optional) |
| Xem danh sách participants | — |
| Approve / Reject verification | Resolve report mà không qua evidence |

### 2.2 Leader (Mobile — UserRole `Cleaner` + `event.leaderUserId == me`)

| Được | Không |
|------|--------|
| Nhận assignment làm Leader | Tự đổi Leader |
| Xem participants | Resolve report trực tiếp |
| Check-in, before/progress/after, cập nhật % | Hủy chương trình (chỉ LEO) |
| Submit verification | Join như Citizen trên chính event mình lead (optional cho phép — mặc định Leader tự là participant kiểu Leader) |

### 2.3 Citizen / Participant (Mobile — UserRole `Citizen`)

| Được | Không |
|------|--------|
| Xem event đang mở (list/map/detail) | Cập nhật tiến độ |
| Join / Withdraw (trước khi event InProgress hoặc theo rule) | Submit verification |
| Check-in ngày dọn | Upload after thay Leader |
| Xem tiến độ read-only | Thấy PII participant khác (chỉ số đếm công khai) |

### 2.4 Guest (chưa login)

Chỉ xem map/event công khai read-only nếu product cho phép — **Join bắt buộc login** (BR-AUTH-017 tinh thần).

---

## 3. So với Cleanup Team hiện tại

| | Assign Team (hiện có) | Community Cleanup (mới) |
|--|----------------------|-------------------------|
| Executor | `EnvironmentalTeam` members | Crowd Citizen + 1 Leader Cleaner |
| Vào việc | LEO assign → Accept/Decline | LEO mở event → Citizen **Join** |
| Tiến độ | Team members (BR-CLN) | **Chỉ Leader** |
| Resolve | Team resolve (ảnh after) | Leader submit → **LEO approve** |
| Entity chính | `ReportAssignment` | `CommunityCleanupEvent` + `Participant` |

Khi report có Community Cleanup **active** → `POST assign` / `assign-company` trả **409/422 Conflict**.

---

## 4. State machines

### 4.1 `CommunityCleanupEvent.Status`

```
OpenForJoin
    │ đóng đăng ký (LEO hoặc đủ max / hết hạn)
    ▼
JoinClosed
    │ Leader check-in (hoặc StartsAt tới + Leader start)
    ▼
InProgress
    │ Leader submit verification
    ▼
PendingVerification
    ├─ LEO Approve ──► Completed  (+ Report → Resolved)
    └─ LEO Reject  ──► InProgress (kèm lý do ≥ 20 ký tự)
    
Mọi trạng thái (trừ Completed) có thể → Cancelled (LEO)
```

| Status | Ý nghĩa |
|--------|---------|
| `OpenForJoin` | Citizen được Join |
| `JoinClosed` | Không join thêm; chờ / chuẩn bị dọn |
| `InProgress` | Đang dọn; Leader cập nhật tiến độ |
| `PendingVerification` | Chờ LEO |
| `Completed` | Xong |
| `Cancelled` | Hủy |

### 4.2 `CommunityCleanupParticipant.Status`

```
Joined ──► CheckedIn
   │
   └──► Withdrawn   (trước InProgress hoặc trong cửa sổ cho phép)
   
Joined / CheckedIn ──► NoShow  (Leader/LEO đánh dấu sau StartsAt)
```

**Không có** `Pending` / `Rejected` ở MVP (Join = làm ngay).

### 4.3 `Report.Status` khi dùng community

| Bước | Report status |
|------|----------------|
| Trước khi tạo event | `Verified` (bắt buộc) |
| LEO tạo event thành công | `Verified` → `InProgress` (qua method domain mới, **không** gọi AssignTeam) |
| LEO Approve verification | `InProgress` → `Resolved` |
| Event Cancelled (chưa Resolved) | `InProgress` → `Verified` (re-open điều phối) **hoặc** giữ InProgress — **chốt: về Verified** để gán lại |

> Implement trong `Report` entity method kiểu `StartCommunityCleanup(leoId)` / `ResolveFromCommunityCleanup(leoId)` — **không** public setter Status.

---

## 5. Domain model đề xuất

### 5.1 `CommunityCleanupEvent`

| Field | Type | Note |
|-------|------|------|
| `Id` | Guid | PK |
| `ReportId` | Guid | FK Report — unique active event per report |
| `CreatedByLeoId` | Guid | LEO tạo |
| `LeaderUserId` | Guid | Cleaner |
| `LeaderTeamId` | Guid | Team của Leader lúc assign (audit) |
| `Status` | enum | xem §4.1 |
| `Title` | string(200) | |
| `Description` | string(2000)? | |
| `JoinOpensAt` | DateTime | mặc định = CreatedAt |
| `JoinClosesAt` | DateTime? | null = đóng tay |
| `StartsAt` | DateTime | giờ họp / bắt đầu dọn |
| `EndsAt` | DateTime? | |
| `MaxParticipants` | int | default 50, max 200 |
| `MeetingNote` | string(500)? | điểm tập trung |
| `MeetingLatitude` | decimal? | optional khác report GPS |
| `MeetingLongitude` | decimal? | |
| `ProgressPercent` | int | 0–100, Leader cập nhật |
| `ProgressNote` | string? | |
| `ProgressUpdatedAt` | DateTime? | |
| `SubmittedAt` | DateTime? | lúc Leader submit verify |
| `VerifiedAt` | DateTime? | LEO approve |
| `VerifiedByLeoId` | Guid? | |
| `RejectionReason` | string? | |
| `CancelledAt` / `CancelReason` | | |
| Audit | CreatedAt, UpdatedAt, … | AuditableEntity |

**Indexes:** `(ReportId)`, `(Status, StartsAt)`, `(LeaderUserId, Status)`, unique partial: một event active / report (`Status NOT IN (Completed, Cancelled)`).

### 5.2 `CommunityCleanupParticipant`

| Field | Type | Note |
|-------|------|------|
| `Id` | Guid | |
| `EventId` | Guid | |
| `UserId` | Guid | Citizen (hoặc Cleaner khác — MVP chỉ Citizen join) |
| `Status` | enum | Joined / CheckedIn / Withdrawn / NoShow |
| `Role` | enum | `Member` \| `Leader` (Leader cũng có 1 row) |
| `JoinedAt` | DateTime | |
| `CheckedInAt` | DateTime? | |
| `CheckInLatitude` / `Longitude` | decimal? | |
| Unique | `(EventId, UserId)` | |

### 5.3 Media

Reuse `ReportMedia` trên **ReportId** của báo cáo:

| Phase | `MediaType` đề xuất |
|-------|---------------------|
| Before (Leader) | `Before` |
| Progress | `Progress` |
| After | `After` |

Optional P1: cột `CommunityCleanupEventId` trên media nếu muốn tách gallery event — MVP không bắt buộc.

### 5.4 Enums mới

```csharp
public enum CommunityCleanupStatus
{
    OpenForJoin,
    JoinClosed,
    InProgress,
    PendingVerification,
    Completed,
    Cancelled
}

public enum CommunityCleanupParticipantStatus
{
    Joined,
    CheckedIn,
    Withdrawn,
    NoShow
}

public enum CommunityCleanupParticipantRole
{
    Leader,
    Member
}
```

### 5.5 NotificationType bổ sung (gợi ý)

```
CommunityCleanupLeaderAssigned
CommunityCleanupOpenedNearby      // P1
CommunityMemberJoined             // → Leader
CommunityCleanupStartingSoon
CommunityProgressUpdated
CommunityPendingVerification      // → LEO
CommunityVerificationApproved
CommunityVerificationRejected
CommunityCleanupCancelled
```

---

## 6. Business rules (draft — đặt ID tạm)

> Prefixed `BR-CMU-*` tạm. Khi merge vào BR doc chính thức thì đổi ID.

| ID tạm | Rule |
|--------|------|
| BR-CMU-001 | Chỉ LEO (cùng LocalOffice với report) tạo event trên report `Verified` |
| BR-CMU-002 | Leader phải là User role `Cleaner` + `TeamMember` của một `EnvironmentalTeam` type Cleanup, active |
| BR-CMU-003 | 1 report chỉ 1 event active; không AssignTeam/Company khi active |
| BR-CMU-004 | Join chỉ khi `OpenForJoin` và `participants < MaxParticipants` |
| BR-CMU-005 | Join = Member ngay (`Joined`); user đã join không join lại |
| BR-CMU-006 | Withdraw chỉ khi status event ∈ {OpenForJoin, JoinClosed} và chưa CheckedIn |
| BR-CMU-007 | Check-in: GPS ≤ 200m tới report (hoặc meeting point nếu có) — reuse BR-CLN-002 |
| BR-CMU-008 | Chỉ Leader cập nhật progress / upload before-progress-after chính thức |
| BR-CMU-009 | Submit verification: ≥ 1 before + ≥ 2 after (hash khác) — tinh thần BR-CLN-005 |
| BR-CMU-010 | Chỉ LEO Approve/Reject PendingVerification; Approve → Report Resolved |
| BR-CMU-011 | Reject lý do ≥ 20 ký tự → event về InProgress |
| BR-CMU-012 | Cancel: noti tất cả; report về Verified nếu chưa Resolved |
| BR-CMU-013 | Anti-spam noti progress: max 20/type/ngày (BR-NTF-003) |
| BR-CMU-014 | Guest không Join (BR-AUTH-017) |
| BR-CMU-015 | Public list không trả PII participants — chỉ `participantCount`, `spotsLeft` |

---

## 7. API Contract

Base: `/v1/community-cleanups`  
Envelope: `{ code, message, status, data }`  
Auth: JWT. Policy gợi ý: `CanManageCommunityCleanup` (LEO/Admin), `CanLeadCommunityCleanup` (Cleaner leader of event).

### 7.1 LEO — Web

#### Tạo chương trình

```http
POST /v1/reports/{reportId}/community-cleanups
Authorization: Bearer {leo}
Content-Type: application/json

{
  "title": "Dọn rác kênh Nhiêu Lộc — Cộng đồng",
  "description": "Mang găng tay, nước uống. Tập trung cổng công viên.",
  "leaderUserId": "guid-cleaner",
  "joinClosesAt": "2026-08-01T10:00:00Z",
  "startsAt": "2026-08-01T14:00:00Z",
  "endsAt": "2026-08-01T17:00:00Z",
  "maxParticipants": 40,
  "meetingNote": "Cổng công viên 23/9",
  "meetingLatitude": 10.782,
  "meetingLongitude": 106.695
}
```

**201 data:** event detail (xem §7.4)

**Errors:**  
- `REPORT_NOT_VERIFIED`  
- `COMMUNITY_ALREADY_ACTIVE`  
- `LEADER_NOT_CLEANER` / `LEADER_NOT_IN_CLEANUP_TEAM`  
- `REPORT_HAS_ACTIVE_ASSIGNMENT` (đã assign team/company)

Side effects: Report → InProgress; tạo Participant Leader; noti Leader.

#### Đóng đăng ký

```http
POST /v1/community-cleanups/{eventId}/close-join
```

#### Hủy

```http
POST /v1/community-cleanups/{eventId}/cancel
{ "reason": "Thời tiết xấu, dời lịch tuần sau..." }  // ≥ 20 ký tự
```

#### Duyệt / từ chối verification

```http
POST /v1/community-cleanups/{eventId}/verify
{}  // approve

POST /v1/community-cleanups/{eventId}/reject-verification
{ "reason": "Ảnh after chưa rõ khu vực đã dọn, vui lòng chụp lại..." }
```

#### Queue LEO

```http
GET /v1/community-cleanups/office-queue?status=PendingVerification&page=1&pageSize=20
```

#### Đổi Leader (P1)

```http
PUT /v1/community-cleanups/{eventId}/leader
{ "leaderUserId": "guid" }
```

### 7.2 Citizen — Mobile

#### List đang mở (paged)

```http
GET /v1/community-cleanups?status=OpenForJoin&nearLat=10.78&nearLng=106.70&radiusMeters=5000&page=1&pageSize=20
```

#### Detail

```http
GET /v1/community-cleanups/{eventId}
```

#### Join (= Vote)

```http
POST /v1/community-cleanups/{eventId}/join
```

**200/201:** participant self  
**Errors:** `JOIN_CLOSED`, `EVENT_FULL`, `ALREADY_JOINED`, `NOT_CITIZEN` (nếu restrict)

#### Withdraw

```http
POST /v1/community-cleanups/{eventId}/withdraw
```

#### My joins

```http
GET /v1/community-cleanups/my?page=1&pageSize=20
```

#### Check-in (Citizen hoặc Leader)

```http
POST /v1/community-cleanups/{eventId}/check-in
{ "latitude": 10.7821, "longitude": 106.6952 }
```

### 7.3 Leader — Mobile

#### Event của tôi (leader)

```http
GET /v1/community-cleanups/led-by-me?status=InProgress&page=1&pageSize=20
```

#### Participants

```http
GET /v1/community-cleanups/{eventId}/participants?page=1&pageSize=50
```

(Chỉ Leader của event + LEO)

#### Start cleanup (optional explicit)

```http
POST /v1/community-cleanups/{eventId}/start
```

`JoinClosed|OpenForJoin` → `InProgress` (sau check-in Leader)

#### Upload before / progress / after

Reuse pattern presigned URL hiện có (như cleanup assignment):

```http
POST /v1/community-cleanups/{eventId}/media/presign
{ "mediaType": "Before"|"Progress"|"After", "contentType": "image/jpeg", "fileName": "a.jpg" }

POST /v1/community-cleanups/{eventId}/media/confirm
{ "objectKey": "...", "mediaType": "Before", ... }
```

#### Update progress

```http
PUT /v1/community-cleanups/{eventId}/progress
{ "percent": 60, "note": "Đã dọn 2/3 đoạn kênh" }
```

#### Submit verification

```http
POST /v1/community-cleanups/{eventId}/submit-verification
```

Validate: before ≥1, after ≥2, percent ≥ config (default 100 hoặc ≥80 — **chốt: ≥ 100** MVP).

### 7.4 Response mẫu — Event detail

```json
{
  "code": "200",
  "message": "OK",
  "status": "success",
  "data": {
    "id": "evt-...",
    "reportId": "rpt-...",
    "reportCode": "RPT-2026-0045",
    "status": "OpenForJoin",
    "title": "Dọn rác kênh Nhiêu Lộc — Cộng đồng",
    "description": "...",
    "leader": {
      "userId": "cleaner-...",
      "fullName": "Nguyễn A",
      "teamId": "team-...",
      "teamName": "Đội sạch P.Bến Nghé"
    },
    "joinOpensAt": "2026-07-26T10:00:00Z",
    "joinClosesAt": "2026-08-01T10:00:00Z",
    "startsAt": "2026-08-01T14:00:00Z",
    "endsAt": "2026-08-01T17:00:00Z",
    "maxParticipants": 40,
    "participantCount": 12,
    "spotsLeft": 28,
    "progressPercent": 0,
    "progressNote": null,
    "meetingNote": "Cổng công viên 23/9",
    "meetingLatitude": 10.782,
    "meetingLongitude": 106.695,
    "reportLatitude": 10.7815,
    "reportLongitude": 106.6948,
    "reportAddress": "125 Nguyễn Huệ...",
    "categoryName": "Ô nhiễm rác thải",
    "thumbnailUrl": "https://cdn.../thumb.jpg",
    "myParticipation": null,
    "isLeader": false,
    "mediaSummary": {
      "beforeCount": 0,
      "progressCount": 0,
      "afterCount": 0
    }
  }
}
```

Khi user đã join: `"myParticipation": { "status": "Joined", "joinedAt": "...", "role": "Member" }`.

---

## 8. Notifications — catalog đầy đủ

| Type | Trigger | Recipients | Deep-link |
|------|---------|------------|-----------|
| `CommunityCleanupLeaderAssigned` | LEO tạo event | Leader | Leader event detail |
| `CommunityCleanupOpenedNearby` | Publish (P1, radius 2km) | Citizen pref on | Event detail |
| `CommunityMemberJoined` | Citizen join | Leader | Participants |
| `CommunityCleanupStartingSoon` | Job T-2h trước `startsAt` | Leader + Joined/CheckedIn | Event detail |
| `CommunityJoinClosingSoon` | Job T-24h `joinClosesAt` (P1) | Not yet joined nearby / watchers | Event detail |
| `CommunityProgressUpdated` | Leader PUT progress | Members (không Leader) | Event detail |
| `CommunityPendingVerification` | Leader submit | LEO (office) | Web verify queue |
| `CommunityVerificationApproved` | LEO approve | Leader + all participants + report reporter | Report / event |
| `CommunityVerificationRejected` | LEO reject | Leader | Event detail + reason |
| `CommunityCleanupCancelled` | LEO cancel | Leader + participants | Event / home |

**Preferences:** user bật/tắt theo type (BR-NTF-001).  
**Anti-spam:** progress updates gộp / max 20/ngày (BR-NTF-003).  
**FCM data:** `notificationId`, `type`, `eventId`, `reportId`.

---

## 9. Gamification (đề xuất số — chỉnh được)

| Hành động | Ai | Điểm |
|-----------|-----|------|
| Join thành công | Citizen | +2 |
| Check-in hợp lệ | Citizen / Leader | +5 |
| Event Completed (đã check-in) | Citizen | +10 |
| Event Completed với vai Leader | Leader | +20 |
| Report Resolved (community path) | Reporter gốc | +20 (như Resolved hiện tại) |

Badge gợi ý: `Community Helper` (3 events), `Cleanup Leader` (1 lead completed), `Weekend Warrior`.

---

## 10. UI / UX

### 10.1 Web LEO

1. **Report detail** → CTA “Mở dọn cộng đồng” (chỉ Verified, không assignment)  
2. **Modal tạo:** title, mô tả, chọn Leader (dropdown Cleaner theo office/teams), lịch join/start, max people, meeting note  
3. **Queue “Chờ xác thực cộng đồng”**  
4. **Event detail:** participants count, media gallery, Approve / Reject  

### 10.2 Mobile Citizen

| Màn | Nội dung |
|-----|----------|
| Tab/List Community | Event `OpenForJoin` + gần tôi |
| Map pin | Icon khác pin ô nhiễm thường (vd. lá / người) khi có event mở trên report |
| Event detail | Info, map nhỏ, Join, số chỗ còn |
| My joins | Joined / CheckedIn / Completed |
| Noti | Deep-link event |

### 10.3 Mobile Leader (Cleaner app shell hiện có)

| Màn | Nội dung |
|-----|----------|
| “Chương trình tôi dẫn” | Led-by-me list |
| Event workspace | Participants, check-in, upload, progress slider, Submit |
| Noti | Assigned / join / reject verify |

Reuse UI pattern: assignment task screens (before/progress/complete) càng nhiều càng tốt.

---

## 11. Validation & edge cases

| Case | Xử lý |
|------|--------|
| Leader bị soft-delete / rời team | LEO phải reassign Leader trước Start; block submit nếu Leader inactive |
| Đủ max | Join → `EVENT_FULL` |
| Join sau JoinClosed | 422 |
| 2 LEO tạo cùng report | Unique constraint + transaction |
| Report bị Reject / Duplicate sau khi mở event | Auto Cancel event + noti |
| Leader quên update >24h / >48h | Job nhắc (reuse CleanupProgressSla tinh thần) |
| Citizen join rồi LEO cancel | Withdraw auto + noti |
| Ảnh after trùng hash | 422 như BR-CLN-005 |
| Check-in >200m | 422 + message |

---

## 12. Bảo mật & privacy

- Public/Citizen list: **không** trả danh sách tên participant — chỉ count.  
- Participants full list: Leader + LEO.  
- GPS meeting/report: public round theo BR-MAP-004 nếu expose map công khai.  
- Mọi action ghi audit (BR-ADM-010): create, join, submit, verify, cancel.  
- Rate limit Join: vd. 10/h/user (chống spam).

---

## 13. Migration / schema checklist

- [ ] Table `community_cleanup_events`  
- [ ] Table `community_cleanup_participants`  
- [ ] Enums + EF configs + soft delete  
- [ ] Index unique active event per `report_id`  
- [ ] Optional: `reports.community_cleanup_active` denormalized bool (perf)  
- [ ] Seed NotificationTemplate cho types mới  
- [ ] Block AssignTeam handlers khi event active  

---

## 14. Vertical slices đề xuất (Application)

```
Application/Features/CommunityCleanup/
  CreateCommunityCleanup/
  CloseJoin/
  CancelCommunityCleanup/
  JoinCommunityCleanup/
  WithdrawCommunityCleanup/
  CheckInCommunityCleanup/
  UpdateCommunityProgress/
  PresignCommunityMedia/ + ConfirmCommunityMedia/
  SubmitCommunityVerification/
  VerifyCommunityCleanup/      // LEO approve
  RejectCommunityVerification/
  GetCommunityCleanupById/
  GetOpenCommunityCleanups/
  GetMyCommunityCleanups/
  GetLedCommunityCleanups/
  GetCommunityParticipants/
  GetOfficeCommunityQueue/
```

Background jobs:

- `CommunityCleanupStartingSoonJob` (every 15m)  
- `CommunityJoinClosingSoonJob` (optional)  
- `CommunityProgressSlaJob` (optional)

---

## 15. Test plan (BR-tagged mẫu)

```
Create_WhenReportVerified_StartsInProgress_BR_CMU_001
Create_WhenLeaderNotCleaner_ReturnsError_BR_CMU_002
Create_WhenAssignmentExists_ReturnsConflict_BR_CMU_003
Join_WhenOpen_AddsParticipant_BR_CMU_004
Join_WhenFull_ReturnsError_BR_CMU_004
CheckIn_WhenFartherThan200m_ReturnsError_BR_CMU_007
Submit_WithoutTwoAfterPhotos_ReturnsError_BR_CMU_009
Verify_ByLeo_ResolvesReport_BR_CMU_010
AssignTeam_WhenCommunityActive_ReturnsConflict_BR_CMU_003
```

---

## 16. Milestone triển khai

| Phase | Deliverable | Owner gợi ý |
|-------|-------------|-------------|
| **M0** | Spec này + chốt BR ID với supervisor | PM / bạn |
| **M1** | Schema + Create + block AssignTeam + GetById | BE |
| **M2** | Join / Withdraw / My / Open list | BE + Mobile Citizen |
| **M3** | Leader progress + media + submit | BE + Mobile Leader |
| **M4** | LEO verify/reject + resolve report | BE + Web LEO |
| **M5** | Notifications + jobs starting-soon | BE + Mobile |
| **M6** | Points/badges + map pins | BE + Mobile |
| **M7** | Polish: nearby open, SLA nhắc Leader, đổi Leader | P1 |

---

## 17. Open options (P1 — chưa khóa, có thể bật sau)

| Option | Default MVP | P1 |
|--------|-------------|-----|
| Auto-close join khi `startsAt` | Có (job) | — |
| Leader duyệt join | **Tắt** (join ngay) | Bật `requiresApproval` |
| Citizen Cleaner khác join như Member | Cho phép hoặc chặn — **MVP: chỉ Citizen** | Cho Cleaner |
| Meeting GPS khác report GPS | Optional fields | Bắt buộc |
| Chat nhóm event | Không | Có |
| Chứng nhận PDF hoàn thành | Không | Có |
| Nhiều Leader | Không (1 Leader) | Co-leader |
| Recurring events | Không | Có |

---

## 18. Acceptance criteria (MVP M1–M5)

- [ ] LEO tạo được chương trình trên report Verified, chọn Leader Cleaner  
- [ ] Report chuyển InProgress; Assign Team bị chặn  
- [ ] Citizen Join (= Vote) thành Member; thấy spotsLeft giảm  
- [ ] Leader cập nhật tiến độ + ảnh; Citizen chỉ xem  
- [ ] Leader submit → LEO thấy queue → Approve → Report Resolved + noti mọi phía  
- [ ] Reject → Leader nhận lý do, event lại InProgress  
- [ ] Cancel → participants được noti; report về Verified  
- [ ] Check-in ≤ 200m enforce  
- [ ] Không lộ danh sách tên participant cho user lạ  

---

## 19. File / folder gợi ý khi code

**BE**

- `Domain/Entities/CommunityCleanupEvent.cs`  
- `Domain/Entities/CommunityCleanupParticipant.cs`  
- `Domain/Enums/CommunityCleanup*.cs`  
- `Application/Features/CommunityCleanup/**`  
- `Infrastructure/Persistence/Configurations/CommunityCleanup*`  
- Migration `yyyyMMddHHmm_AddCommunityCleanup`

**Mobile**

- `app/(tabs)/community.tsx` hoặc section trong home  
- `app/community/[id].tsx`  
- `app/(staff)/community-lead/*` (Leader trong shell Cleaner)  
- `src/services/communityCleanup.service.ts`  
- `src/types/community-cleanup.types.ts`

**Web LEO**

- Report detail CTA + Create modal  
- Queue “Community verification”

---

## 20. Glossary

| Thuật ngữ | Nghĩa trong feature này |
|-----------|-------------------------|
| Chương trình / Event | `CommunityCleanupEvent` |
| Vote | = Join |
| Leader | Cleaner được LEO chỉ định, cập nhật tiến độ |
| Participant / Member | Citizen đã Join |
| Verification | Leader nộp after → LEO duyệt |
| Thay Assign Team | Không dùng `ReportAssignment` cho report đang có event active |

---

**Hết spec.**  
Dùng file này làm source of truth khi implement. Khi BR chính thức có ID, thay `BR-CMU-*` tạm và cập nhật §6 + test names.

**Phiên bản:** 1.0 · 2026-07-26 · Khóa theo Q&A product (Leader Cleaner, Join=Vote, thay Assign, LEO verify, Mobile user+leader / Web LEO).

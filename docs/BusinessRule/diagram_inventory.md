# GreenLens — Danh mục đầy đủ Class Diagram & Sequence Diagram

> Dựa trên phân tích mã nguồn thực tế của hệ thống, bao gồm **42 Domain Entities**, **18 Enums**, **12 Feature modules**, **17 API Controllers**, và **150+ use cases**.

---

## Phần A: Class Diagram — Theo Module / Bounded Context

### Tổng quan phân chia

| # | Module (Bounded Context) | Số entity chính | Mô tả |
|---|--------------------------|-----------------|--------|
| 1 | User & Authentication | 5 | Quản lý tài khoản, xác thực, phân quyền |
| 2 | Report (Pollution Report) | 9 | Lõi nghiệp vụ — vòng đời báo cáo ô nhiễm |
| 3 | Organization | 8 | Cơ cấu tổ chức: Tỉnh/Phường, Đội, Công ty |
| 4 | Inspection & Penalty | 4 | Thanh tra, xử phạt vi phạm môi trường |
| 5 | Comment & Interaction | 3 | Bình luận cộng đồng trên báo cáo |
| 6 | Gamification | 5 | Điểm thưởng, huy hiệu, bảng xếp hạng |
| 7 | Notification | 3 | Thông báo, preference, template |
| 8 | Catalog & Location | 5 | Danh mục ô nhiễm, nhãn rác, đơn vị hành chính |

> **Tổng: 8 Class Diagrams**

---

### CD-01: User & Authentication Module

**Mô tả:** Quản lý vòng đời tài khoản, xác thực JWT, OTP, social login.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `User` | Entity (Aggregate Root) | Tài khoản người dùng, role, trạng thái, soft-delete |
| `RefreshToken` | Entity | JWT refresh token rotation (hashed), expiry |
| `OtpCode` | Entity | Mã OTP cho xác thực phone/email, expiry, purpose |
| `PasswordHistory` | Entity | Lịch sử mật khẩu (chống dùng lại) |
| `UserRole` | Enum | Citizen, DEO, LEO, Cleaner, CompanyManager, CompanyStaff, Inspector, Admin |

**Quan hệ chính:**
- `User` 1 ←→ * `RefreshToken`
- `User` 1 ←→ * `OtpCode`
- `User` 1 ←→ * `PasswordHistory`
- `User` has `UserRole` (enum)

---

### CD-02: Report (Pollution Report) Module ⭐ (Quan trọng nhất)

**Mô tả:** Vòng đời báo cáo ô nhiễm — từ Submitted → Verified → InProgress → Resolved → Closed. Bao gồm state machine, media, assignment, flag, satisfaction.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `Report` | Entity (Aggregate Root) | Báo cáo ô nhiễm: title, description, location (PostGIS Point), severity, state machine |
| `ReportMedia` | Entity | Ảnh/video đính kèm báo cáo (URL S3, media type, EXIF metadata) |
| `ReportAssignment` | Entity | Gán đội dọn dẹp/công ty cho báo cáo, trạng thái assignment |
| `ReportStatusHistory` | Entity | Lịch sử thay đổi trạng thái báo cáo (audit trail) |
| `ReportDraft` | Entity | Bản nháp báo cáo chưa submit |
| `ReportFlag` | Entity | Citizen flag báo cáo (trùng lặp, spam, sai) |
| `ReportSatisfaction` | Entity | Đánh giá mức độ hài lòng sau khi báo cáo được giải quyết |
| `ReportWasteTag` | Entity (Join) | Liên kết Report ↔ WasteTag (N:N) |
| `ReportStatus` | Enum | Submitted, Verified, InProgress, Resolved, Closed, Rejected, Duplicate |
| `AssignmentStatus` | Enum | Assigned, InProgress, Completed, Declined, Escalated |
| `Severity` | Enum | Mức độ nghiêm trọng |
| `FlagType` | Enum | Loại flag |

**Quan hệ chính:**
- `Report` 1 ←→ * `ReportMedia`
- `Report` 1 ←→ * `ReportAssignment`
- `Report` 1 ←→ * `ReportStatusHistory`
- `Report` 1 ←→ 0..1 `ReportDraft`
- `Report` 1 ←→ * `ReportFlag`
- `Report` 1 ←→ 0..1 `ReportSatisfaction`
- `Report` * ←→ * `WasteTag` (qua `ReportWasteTag`)
- `Report` → `User` (CreatedBy)
- `Report` → `PollutionCategory`
- `ReportAssignment` → `EnvironmentalTeam`

**State Machine (bắt buộc vẽ trong diagram):**
```
Submitted → Verified → InProgress → Resolved → Closed
    ↓           ↓                       ↑ (max 2 lần)
 Rejected    Duplicate              InProgress (reopen)
```

---

### CD-03: Organization Module

**Mô tả:** Cơ cấu tổ chức hành chính (Tỉnh → Phòng ban → Văn phòng phường), Công ty dịch vụ môi trường, đội dọn dẹp/thanh tra.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `Department` | Entity | Phòng Tài nguyên Môi trường cấp Tỉnh |
| `LocalOffice` | Entity | Văn phòng cấp Phường/Xã |
| `EnvironmentalTeam` | Entity | Đội môi trường (Cleanup hoặc Inspection) |
| `TeamMember` | Entity (Join) | Thành viên đội (User ↔ Team) |
| `EnvironmentalServiceCompany` | Entity (Aggregate Root) | Công ty dịch vụ môi trường |
| `CompanyStaff` | Entity | Nhân viên công ty |
| `CompanyServiceArea` | Entity | Vùng phục vụ của công ty (liên kết với Ward) |
| `ContractPeriod` | Entity | Hợp đồng giữa công ty và văn phòng |
| `StaffInvitation` | Entity | Lời mời tham gia công ty |
| `TeamType` | Enum | Cleanup, Inspection |
| `ContractType` | Enum | Loại hợp đồng |
| `InvitationStatus` | Enum | Pending, Accepted, Declined |

**Quan hệ chính:**
- `Department` 1 ←→ * `LocalOffice`
- `LocalOffice` 1 ←→ * `EnvironmentalTeam`
- `EnvironmentalTeam` 1 ←→ * `TeamMember`
- `TeamMember` → `User`
- `EnvironmentalServiceCompany` 1 ←→ * `CompanyStaff`
- `EnvironmentalServiceCompany` 1 ←→ * `CompanyServiceArea`
- `EnvironmentalServiceCompany` 1 ←→ * `ContractPeriod`
- `ContractPeriod` → `LocalOffice`

---

### CD-04: Inspection & Penalty Module

**Mô tả:** Quy trình thanh tra vi phạm, lập biên bản xử phạt, theo dõi thanh toán.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `InspectionReport` | Entity (Aggregate Root) | Biên bản thanh tra: state machine riêng, liên kết Report + Team |
| `ViolatingEntity` | Entity | Pháp nhân/cá nhân vi phạm |
| `PenaltyPayment` | Entity | Theo dõi thanh toán tiền phạt |
| `PenaltyFramework` | Entity | Khung hình phạt (Admin cấu hình) |
| `InspectionStatus` | Enum | Draft, InProgress, PenaltyIssued, Paid, PartiallyPaid, Overdue, Closed, ClosedNoViolation |
| `ViolationLevel` | Enum | Mức vi phạm |
| `ViolatorType` | Enum | Loại đối tượng vi phạm |

**Quan hệ chính:**
- `InspectionReport` → `Report` (linked)
- `InspectionReport` → `EnvironmentalTeam` (inspection team)
- `InspectionReport` 1 ←→ * `ViolatingEntity`
- `ViolatingEntity` 1 ←→ * `PenaltyPayment`
- `PenaltyFramework` → `PollutionCategory`

**State Machine:**
```
Draft → InProgress → PenaltyIssued → Paid → Closed
                 ↓         ↓
          ClosedNoViolation  PartiallyPaid → Overdue → Closed
```

---

### CD-05: Comment & Community Interaction Module

**Mô tả:** Bình luận cộng đồng trên báo cáo, like, media đính kèm, kiểm duyệt.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `Comment` | Entity (Aggregate Root) | Bình luận trên Report, hỗ trợ reply (nested), soft-delete |
| `CommentMedia` | Entity | Ảnh đính kèm bình luận |
| `CommentLike` | Entity (Join) | Like/unlike bình luận (User ↔ Comment) |
| `BlockedWord` | Entity | Từ cấm dùng cho content moderation |

**Quan hệ chính:**
- `Comment` → `Report`
- `Comment` → `User` (author)
- `Comment` → `Comment` (parentId, self-referencing cho reply)
- `Comment` 1 ←→ * `CommentMedia`
- `Comment` 1 ←→ * `CommentLike`
- `CommentLike` → `User`

---

### CD-06: Gamification Module

**Mô tả:** Hệ thống điểm thưởng, huy hiệu, bảng xếp hạng để khuyến khích tham gia.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `UserPoints` | Entity | Tổng điểm + level hiện tại của User |
| `PointTransaction` | Entity | Lịch sử cộng/trừ điểm |
| `Badge` | Entity | Định nghĩa huy hiệu (tên, điều kiện, icon) |
| `UserBadge` | Entity (Join) | User đã đạt được huy hiệu nào |
| `GamificationConfig` | Entity | Cấu hình điểm cho từng hành động (Admin) |
| `PointReason` | Enum | ReportVerified, ReportResolved, PenaltyIssued, DuplicateReport, ReportRejected, FraudPenalty |
| `LeaderboardPeriod` | Enum | Daily, Weekly, Monthly |

**Quan hệ chính:**
- `User` 1 ←→ 1 `UserPoints`
- `UserPoints` 1 ←→ * `PointTransaction`
- `User` * ←→ * `Badge` (qua `UserBadge`)
- `PointTransaction` → `Report` (optional, nguồn gốc)

---

### CD-07: Notification Module

**Mô tả:** Hệ thống thông báo đa kênh (push, email), preference, template.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `Notification` | Entity | Thông báo gửi tới User (title, body, isRead, deeplink) |
| `NotificationPreference` | Entity | Cài đặt nhận thông báo của user (per type) |
| `NotificationTemplate` | Entity | Template thông báo (Admin quản lý) |
| `NotificationType` | Enum | ReportStatusChanged, NewComment, BadgeEarned, LevelUp, SlaBreachWarning, ... (12 loại) |
| `NotificationChannel` | Enum | Push, Email, InApp |

**Quan hệ chính:**
- `User` 1 ←→ * `Notification`
- `User` 1 ←→ * `NotificationPreference`
- `NotificationTemplate` has `NotificationType`

---

### CD-08: Catalog & Location Module

**Mô tả:** Dữ liệu tham chiếu — danh mục ô nhiễm, nhãn rác, đơn vị hành chính Việt Nam.

**Entities & Classes:**

| Class | Loại | Mô tả |
|-------|------|--------|
| `PollutionCategory` | Entity | Danh mục loại ô nhiễm (Admin quản lý) |
| `WasteTag` | Entity | Nhãn phân loại rác thải (tagging system) |
| `Province` | Entity | Tỉnh/Thành phố |
| `Ward` | Entity | Phường/Xã |
| `AdministrativeRegion` | Entity | Vùng hành chính |
| `AdministrativeUnit` | Entity | Đơn vị hành chính |
| `AuditLog` | Entity | Nhật ký kiểm toán hệ thống |

**Quan hệ chính:**
- `Province` 1 ←→ * `Ward`
- `Ward` → `AdministrativeUnit`
- `Province` → `AdministrativeRegion`
- `Report` → `PollutionCategory`
- `Report` * ←→ * `WasteTag`

---
---

## Phần B: Sequence Diagram — Theo Use Case / Luồng nghiệp vụ

### Tổng quan phân nhóm

| # | Nhóm luồng | Số Sequence Diagram | Actor chính |
|---|------------|---------------------|-------------|
| 1 | Authentication & Account | 8 | Citizen, All |
| 2 | Report Lifecycle (Core) | 12 | Citizen, LEO |
| 3 | Cleanup & Field Work | 7 | Cleaner, CompanyStaff |
| 4 | Inspection & Penalty | 8 | Inspector, LEO |
| 5 | Organization Management | 8 | DEO, Admin, CompanyManager |
| 6 | Comment & Community | 4 | Citizen |
| 7 | Gamification | 4 | System, Citizen |
| 8 | Notification | 3 | System |
| 9 | Map & Public Data | 2 | Citizen, Anonymous |
| 10 | Administration | 6 | Admin |
| 11 | Media & File Upload | 3 | All |

> **Tổng: ~65 Sequence Diagrams** (đầy đủ), nhưng cho bảo vệ tốt nghiệp nên tập trung **20–25 diagram quan trọng nhất** (đánh dấu ⭐).

---

### Nhóm 1: Authentication & Account (8 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-01 ⭐ | **Register (Email + OTP)** | Citizen | Citizen đăng ký → RequestOtp → VerifyOtp → CreateAccount | BR-AUTH-001, BR-AUTH-003 |
| SD-02 ⭐ | **Login (Email/Password)** | All | Nhập email+password → Validate → Issue JWT + RefreshToken → Return tokens | BR-AUTH-005, BR-AUTH-011, BR-AUTH-013 |
| SD-03 | **Google Login (OAuth)** | Citizen | Redirect Google → Callback → FindOrCreate User → Issue JWT | BR-AUTH-002 |
| SD-04 ⭐ | **Refresh Token** | All | Access token expired → Send refresh → Rotate → New pair | BR-AUTH-013 |
| SD-05 | **Forgot Password** | All | Request reset → Send OTP → Verify → ResetPassword | BR-AUTH-006 |
| SD-06 | **Change Password** | All | Verify old password → Update → Invalidate sessions | BR-AUTH-005 |
| SD-07 | **Request Account Deletion** | Citizen | Soft delete → 90 ngày → Hard delete (Background Job) | BR-AUTH-022 |
| SD-08 | **Restore Account** | Citizen | Khôi phục tài khoản trong thời gian 90 ngày | BR-AUTH-022 |

---

### Nhóm 2: Report Lifecycle — Core ⭐ (12 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-09 ⭐ | **Submit Pollution Report** | Citizen | Upload ảnh → Fill form → Validate GPS trong VN → AI analyze → Submit → Auto-route to LocalOffice | BR-REP-001, BR-REP-003, BR-REP-005, BR-REP-013 |
| SD-10 | **Save & Delete Draft** | Citizen | Lưu bản nháp → Xóa bản nháp | BR-REP-019 |
| SD-11 ⭐ | **Verify Report** | LEO | LEO mở queue → Review → Verify (hoặc Reject) → Notify Citizen | BR-REP-020, BR-REP-021 |
| SD-12 ⭐ | **Reject Report** | LEO | LEO reject (lý do ≥ 20 ký tự) → Status = Rejected → Notify Citizen → Trừ điểm | BR-REP-021 |
| SD-13 ⭐ | **Assign Team** | LEO | LEO chọn team → Assign → Status = InProgress → Notify Team | BR-OFF-001, BR-CLN-001 |
| SD-14 | **Dispatch to Company** | LEO | LEO chọn Công ty → Assign → CompanyManager nhận → Phân công CompanyStaff | — |
| SD-15 ⭐ | **Resolve Report** | Cleaner / CompanyStaff | Upload ảnh after → Mark Resolved → Status = Resolved → Notify Citizen | BR-CLN-004, BR-REP-020 |
| SD-16 ⭐ | **Close Report** | Citizen / System | Citizen confirm OR Auto-close sau 7 ngày → Status = Closed → Cộng điểm | BR-REP-016, BR-REP-025 |
| SD-17 | **Reopen Report** | Citizen | Citizen không hài lòng → Reopen (max 2 lần) → Status = InProgress | BR-REP-020 |
| SD-18 ⭐ | **Duplicate Detection & Handling** | AI / LEO | AI detect duplicate → Flag → LEO review → Confirm/Dismiss duplicate | BR-REP-030, BR-REP-032, BR-AI-002 |
| SD-19 | **Flag Report** | Citizen | Citizen flag báo cáo (spam/trùng) → 3+ flags → Notify LEO | BR-REP-033 |
| SD-20 | **Escalate Report** | LEO | Báo cáo vượt khả năng → Escalate lên cấp trên | BR-CLN-006 |

---

### Nhóm 3: Cleanup & Field Work (7 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-21 ⭐ | **Accept / Decline Assignment** | Cleaner / CompanyStaff | Nhận notification → Accept hoặc Decline → Update AssignmentStatus | BR-CLN-001 |
| SD-22 ⭐ | **Check-in at Cleanup Site** | Cleaner / CompanyStaff | GPS check-in (≤ 200m) → Upload ảnh before → Start cleanup | BR-CLN-002 |
| SD-23 | **Update Cleanup Progress** | Cleaner / CompanyStaff | Upload ảnh progress → Update % completion | — |
| SD-24 | **Upload Before Images** | Cleaner / CompanyStaff | Chụp ảnh hiện trạng trước khi dọn | BR-CLN-003 |
| SD-25 | **Escalate Cleanup** | Cleaner | Vượt khả năng → Escalate → Notify LEO | BR-CLN-006 |
| SD-26 | **Reassign Team** | LEO | LEO reassign team khác → Notify cả 2 team | BR-OFF-005 |
| SD-27 | **View My Assignments** | Cleaner / CompanyStaff | Xem danh sách task được giao + filter theo status | — |

---

### Nhóm 4: Inspection & Penalty (8 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-28 ⭐ | **Create Inspection Report** | LEO | LEO tạo biên bản thanh tra từ Report đã Verified | BR-INS-001 |
| SD-29 ⭐ | **Assign Inspection Team** | LEO | Gán đội thanh tra → Team nhận task | BR-INS-002 |
| SD-30 | **Check-in Inspection Site** | Inspector | GPS check-in tại hiện trường → Upload evidence | BR-INS-004 |
| SD-31 | **Update Inspection Progress** | Inspector | Cập nhật tiến độ, ghi nhận chứng cứ | — |
| SD-32 ⭐ | **Issue Penalty** | Inspector / LEO | Lập biên bản xử phạt → Tạo ViolatingEntity → Issue penalty amount | BR-INS-005, BR-INS-006 |
| SD-33 | **Record Payment** | LEO | Ghi nhận thanh toán tiền phạt → Update trạng thái | BR-INS-008 |
| SD-34 | **Close Inspection — No Violation** | Inspector | Không phát hiện vi phạm → Đóng biên bản (lý do) | BR-INS-013 |
| SD-35 | **Mark Overdue Penalty** | System (Job) | Background job check quá hạn → Mark Overdue → Notify | — |

---

### Nhóm 5: Organization Management (8 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-36 ⭐ | **Create Department & LocalOffice** | Admin | Tạo cơ cấu Phòng TNMT → Văn phòng phường | BR-ORG-001 |
| SD-37 ⭐ | **Create Team (Cleanup / Inspection)** | LEO | Tạo đội môi trường + Thêm thành viên | BR-ORG-013 |
| SD-38 ⭐ | **Onboard Environmental Company** | Admin / DEO | Tạo công ty → Cấu hình vùng phục vụ → Tạo hợp đồng | — |
| SD-39 | **Recruit & Release Staff** | CompanyManager | Mời nhân viên (qua email) → Accept → Join company | — |
| SD-40 | **Renew / Terminate Contract** | DEO / Admin | Gia hạn hoặc chấm dứt hợp đồng công ty | — |
| SD-41 | **Assign LEO to Office** | DEO | Gán LEO phụ trách văn phòng phường | — |
| SD-42 | **Transfer Team Member** | LEO | Chuyển thành viên giữa các đội | — |
| SD-43 | **Suspend / Reactivate Company** | DEO | Tạm ngưng hoặc kích hoạt lại công ty | — |

---

### Nhóm 6: Comment & Community (4 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-44 ⭐ | **Add Comment (with Media)** | Citizen | Viết bình luận + upload ảnh → Content moderation → Publish | BR-CMT-001, BR-CMT-002 |
| SD-45 | **Edit / Delete Comment** | Citizen | Sửa/xóa bình luận (chỉ owner) | BR-CMT-003 |
| SD-46 | **Toggle Like Comment** | Citizen | Like / unlike bình luận | — |
| SD-47 | **Hide Comment (Moderation)** | LEO / Admin | Ẩn bình luận vi phạm | BR-CMT-005 |

---

### Nhóm 7: Gamification (4 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-48 ⭐ | **Award Points (Event-driven)** | System | Report verified/resolved → Event → AwardPoints → Check level up → Notify | BR-GAM-001, BR-GAM-002 |
| SD-49 | **Check & Award Badges** | System | Sau mỗi event → Check điều kiện badge → Award nếu đủ | BR-GAM-003 |
| SD-50 | **View Leaderboard** | Citizen | Xem bảng xếp hạng (daily/weekly/monthly) | BR-GAM-005 |
| SD-51 | **Lock Gamification (Fraud)** | Admin | Phát hiện gian lận → Lock điểm → Trừ toàn bộ | BR-GAM-006 |

---

### Nhóm 8: Notification (3 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-52 ⭐ | **Send Notification (Event-driven)** | System | Domain event → Check preference → Render template → Push FCM / Email | BR-NTF-001, BR-NTF-002 |
| SD-53 | **Update Notification Preferences** | All | User bật/tắt loại thông báo | BR-NTF-001 |
| SD-54 | **Mark Read / Mark All Read** | All | Đánh dấu đã đọc | — |

---

### Nhóm 9: Map & Public Data (2 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-55 ⭐ | **View Public Map (Reports + Heatmap)** | Citizen / Anonymous | Mở bản đồ → Query viewport (bbox) → PostGIS → Redis cache 10' → Return markers + heatmap | BR-MAP-001, BR-MAP-004, BR-MAP-012 |
| SD-56 | **Get Map Viewport Summary** | Citizen | Xem tổng hợp báo cáo theo viewport (số lượng, loại ô nhiễm) | — |

---

### Nhóm 10: Administration (6 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-57 | **Manage Pollution Categories** | Admin | CRUD danh mục ô nhiễm | BR-ADM-001 |
| SD-58 | **Manage Waste Tags** | Admin | CRUD nhãn rác thải | — |
| SD-59 | **Ban / Unban User** | Admin | Khóa/mở khóa tài khoản | BR-AUTH-011 |
| SD-60 | **Update User Role** | Admin | Thay đổi role người dùng | — |
| SD-61 | **Force Update Report Status** | Admin | Override trạng thái báo cáo (trường hợp khẩn cấp) | — |
| SD-62 ⭐ | **View Audit Logs** | Admin | Xem nhật ký kiểm toán hệ thống | BR-ADM-010 |
| SD-63 | **Content Moderation & Spam Dashboard** | Admin | Xem dashboard spam, quản lý blocked words | BR-CMT-005 |
| SD-64 | **Configure Gamification** | Admin | Cấu hình điểm/huy hiệu/khung phạt | BR-GAM-001 |
| SD-65 | **Manage Notification Templates** | Admin | CRUD template thông báo | — |

---

### Nhóm 11: Media & File Upload (3 diagrams)

| # | Sequence Diagram | Actor | Mô tả luồng | BR liên quan |
|---|-----------------|-------|--------------|--------------|
| SD-66 ⭐ | **Upload Report Image (Presigned URL)** | Citizen | Request presigned URL → Upload direct to S3 → Confirm → Strip EXIF → AI analyze | BR-REP-001, BR-REP-002, BR-AI-007 |
| SD-67 | **Upload Comment Image** | Citizen | Request presigned URL → Upload → Attach to comment | BR-CMT-002 |
| SD-68 | **Upload User Avatar** | All | Upload avatar → Resize → Store | — |

---
---

## Phần C: Đề xuất cho bài bảo vệ tốt nghiệp

### Class Diagram — Vẽ đủ 8 diagram

Tất cả 8 Class Diagram đều nên có trong tài liệu vì mỗi diagram chỉ chứa 3–12 class, rất gọn.

### Sequence Diagram — Chọn 20–25 diagram quan trọng nhất (đánh dấu ⭐)

| Ưu tiên | Diagram IDs | Lý do |
|---------|-------------|-------|
| **Bắt buộc** (Core flow) | SD-09, SD-11, SD-12, SD-13, SD-15, SD-16, SD-18 | Đây là vòng đời chính của Report — lõi hệ thống |
| **Bắt buộc** (Auth) | SD-01, SD-02, SD-04 | Đăng ký, đăng nhập, refresh — mọi hệ thống đều cần |
| **Quan trọng** (Field work) | SD-21, SD-22, SD-28, SD-29, SD-32 | Quy trình thực địa + thanh tra xử phạt — nét riêng của hệ thống |
| **Quan trọng** (Cross-cutting) | SD-48, SD-52, SD-55, SD-66 | Gamification, notification, map, media — USP của hệ thống |
| **Nên có** (Org) | SD-36, SD-37, SD-38 | Cơ cấu tổ chức — thể hiện tính phức tạp nghiệp vụ |
| **Nên có** (Community) | SD-44 | Comment có content moderation — tính năng cộng đồng |
| **Tùy chọn** (Admin) | SD-62 | Audit log — security compliance |

> **Tổng đề xuất: 22 Sequence Diagram ⭐** — đủ chi tiết cho bảo vệ tốt nghiệp mà không quá dài.

### Thứ tự trình bày đề xuất

```
1. Auth (SD-01, SD-02, SD-04)
2. Report Core (SD-09 → SD-11 → SD-12 → SD-13 → SD-15 → SD-16 → SD-17 → SD-18)
3. Cleanup (SD-21, SD-22)
4. Inspection (SD-28, SD-29, SD-32)
5. Organization (SD-36, SD-37, SD-38)
6. Community (SD-44)
7. Gamification (SD-48)
8. Notification (SD-52)
9. Map (SD-55)
10. Media (SD-66)
11. Admin (SD-62)
```

Trình bày theo đúng luồng mà người dùng trải nghiệm: **đăng ký → báo cáo → xử lý → dọn dẹp → thanh tra → tương tác cộng đồng → quản trị**.

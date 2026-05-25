# Assignment Flow — Tổng hợp thay đổi cần implement

> Branch: `feature/manage-office`  
> Cập nhật: 2026-05-19

---

## 1. Mục tiêu

Chuyển từ flow cũ (Assignment bắt đầu `InProgress` ngay khi assign) sang flow mới yêu cầu team **Accept trước khi làm việc**, đồng thời:
- Gộp upload ảnh vào API cập nhật tiến độ
- Tự resolve `teamId` từ JWT token (không cần client truyền)
- Lưu audit ai cập nhật tiến độ
- Thêm endpoint xem lịch sử tiến độ theo token

---

## 2. So sánh flow cũ vs mới

```
══════════════════════════════════════════════════════════════════
  FLOW CŨ                          FLOW MỚI
══════════════════════════════════════════════════════════════════
POST /assign                        POST /assign
  Assignment.Status = InProgress      Assignment.Status = Assigned   ← thay đổi
  Assignment.StartedAt = now          Assignment.StartedAt = null    ← thay đổi
  Report.Status → InProgress          Report.Status → InProgress

                                    PUT /accept  ← API MỚI
                                      Assignment: Assigned → InProgress
                                      Assignment.StartedAt = now

PUT /decline                        PUT /decline
  check: Status == InProgress         check: Status == Assigned      ← thay đổi
  revert nếu tất cả Declined          revert nếu tất cả Assigned|Declined ← thay đổi

PUT /progress (text only)           PUT /progress (multipart)        ← thay đổi
  body: teamId, %, note               header: token → teamId tự resolve
                                      form: %, note, images[] (optional)
                                      lưu: ProgressUpdatedByUserId   ← thay đổi

POST /progress/images (riêng)       (xóa endpoint này)               ← bỏ

                                    GET /my-progress ← API MỚI
                                      token → teamId → lịch sử tiến độ

PUT /resolve                        PUT /resolve                     (không đổi logic,
PUT /penalty                        PUT /penalty                      chỉ block nếu Assigned)
```

---

## 3. Danh sách thay đổi chi tiết

### 3.1 Domain — `ReportAssignment.cs`

| # | Thay đổi | Chi tiết |
|---|---|---|
| D1 | `Create()`: `Status = Assigned`, `StartedAt = null` | Bỏ `StartedAt = DateTime.UtcNow`, đổi default status |
| D2 | Thêm method `Accept()` | `Assigned → InProgress`, set `StartedAt = DateTime.UtcNow` |
| D3 | `Decline()`: check `Status == Assigned` | Thay vì `InProgress` |
| D4 | `UpdateProgress()`: thêm param `updatedByUserId` | Lưu thêm `ProgressUpdatedByUserId` |
| D5 | Thêm field `ProgressUpdatedByUserId` (Guid?) | Audit ai cập nhật lần cuối |

### 3.2 Domain — `Report.cs`

| # | Thay đổi | Chi tiết |
|---|---|---|
| D6 | `Assign()`: bỏ `StartedAt = DateTime.UtcNow` | StartedAt chỉ set khi team Accept |
| D7 | `RevertToVerified()`: bỏ `EnsureStatus(InProgress)` | Revert condition: tất cả `Assigned` hoặc `Declined` (không cần check report status cứng) |

### 3.3 Infrastructure — Migration

| # | Thay đổi | Chi tiết |
|---|---|---|
| M1 | Thêm cột `progress_updated_by_user_id` (uuid nullable) vào `report_assignments` | Cho field D5 |

### 3.4 Application — Slice mới `AcceptAssignment/`

Tạo 2 file mới:

```
Features/Reports/AcceptAssignment/
├── AcceptAssignmentCommand.cs           # record không có field (teamId từ token)
└── AcceptAssignmentCommandHandler.cs
```

**Logic handler:**
1. Lấy `userId` từ `ICurrentUser`
2. Gọi `ITeamMemberRepository.GetLeaderByUserIdAsync(userId)` → nếu null → `NOT_TEAM_LEADER`
3. Tìm `Assignment` của team đó trên report → nếu null → `ASSIGNMENT_NOT_FOUND`
4. Kiểm tra `Assignment.Status == Assigned` → nếu không → `INVALID_STATUS_TRANSITION`
5. Gọi `assignment.Accept()`
6. `SaveChangesAsync`

### 3.5 Application — Sửa `UpdateProgress/`

| # | File | Thay đổi |
|---|---|---|
| U1 | `UpdateProgressCommand.cs` | Bỏ field `TeamId`. Thêm `ImageFiles` (danh sách bytes+filename+contentType) |
| U2 | `UpdateProgressCommandHandler.cs` | Resolve `teamId` từ `ICurrentUser → GetLeaderByUserIdAsync`. Upload ảnh nếu có (gọi `IFileStorageService`). Truyền `userId` vào `assignment.UpdateProgress()` |

**Request từ controller sẽ là `multipart/form-data`:**
- `progressPercent` (int, bắt buộc)
- `progressNote` (string, optional)
- `images` (IFormFile[], optional, tối đa 5 file, mỗi file ≤ 20MB)

**Response:** trả về danh sách URL ảnh đã upload (nếu có).

### 3.6 Application — Slice mới `GetMyProgressHistory/`

Tạo 2 file mới:

```
Features/Reports/GetMyProgressHistory/
├── GetMyProgressHistoryQuery.cs
└── GetMyProgressHistoryQueryHandler.cs
```

**Logic handler:**
1. Lấy `userId` từ `ICurrentUser`
2. Gọi `ITeamMemberRepository.GetLeaderByUserIdAsync(userId)` → lấy `teamId`
3. Query tất cả `ReportAssignment` của team đó, có filter optional theo `assignmentStatus`
4. Trả về danh sách kèm thông tin tiến độ

**Response shape mỗi item:**
```json
{
  "reportId": "uuid",
  "reportCode": "RPT-260519-XXXXXX",
  "assignmentId": "uuid",
  "assignmentStatus": "InProgress",
  "reportStatus": "InProgress",
  "progressPercent": 60,
  "progressNote": "Đã dọn xong khu A",
  "progressUpdatedAt": "2026-05-19T10:00:00Z",
  "progressUpdatedByUserId": "uuid",
  "assignedAt": "2026-05-19T08:00:00Z",
  "startedAt": "2026-05-19T08:30:00Z"
}
```

**Query params:** `page`, `pageSize`, `assignmentStatus` (optional filter)

### 3.7 Application — Sửa `DeclineAssignment/`

| # | File | Thay đổi |
|---|---|---|
| DC1 | `DeclineAssignmentCommandHandler.cs` dòng 41 | Đổi check từ `InProgress` → `Assigned` |
| DC2 | `DeclineAssignmentCommandHandler.cs` dòng 50-53 | Điều kiện revert: tất cả assignments phải là `Assigned` **hoặc** `Declined` |

### 3.8 Api — `ReportsController.cs`

| # | Thay đổi | Chi tiết |
|---|---|---|
| A1 | Thêm `PUT {id}/accept` | Gọi `AcceptAssignmentCommand`, role `Cleanup,Inspector,Admin` |
| A2 | Sửa `PUT {id}/progress` | Đổi sang `multipart/form-data`, bỏ `teamId` khỏi body, thêm `images[]` |
| A3 | Thêm `GET my-progress` | Gọi `GetMyProgressHistoryQuery`, role `Cleanup,Inspector,Admin` |
| A4 | Xóa `POST {id}/progress/images` | Đã gộp vào `/progress` |

### 3.9 Infrastructure — `ITeamMemberRepository` & `IReportAssignmentRepository`

| # | Interface | Method cần thêm |
|---|---|---|
| R1 | `IReportAssignmentRepository` | `GetByTeamIdAsync(Guid teamId, AssignmentStatus? status, int page, int pageSize, CancellationToken ct)` |

---

## 4. State machine sau khi thay đổi

```
Report.Status:
  Verified → InProgress (lúc Officer assign)
  InProgress → Verified  (nếu tất cả Assignment Assigned|Declined)
  InProgress → Resolved  (nếu tất cả non-Declined Assignment Completed)

Assignment.Status:
  [mới tạo]  = Assigned
  Assigned   → InProgress   (khi team Accept)
  Assigned   → Declined     (khi team Decline, trong 2h)
  InProgress → Completed    (khi team Resolve/Penalty)
```

---

## 5. Thứ tự implement

```
1. D1–D7   → sửa Domain entities (không có dependency)
2. M1      → tạo migration mới
3. R1      → bổ sung IReportAssignmentRepository
4. 3.4     → slice AcceptAssignment (mới hoàn toàn)
5. DC1–DC2 → sửa DeclineAssignment handler
6. U1–U2   → sửa UpdateProgress (gộp ảnh + token)
7. 3.6     → slice GetMyProgressHistory (mới hoàn toàn)
8. A1–A4   → sửa controller
```

---

## 6. Error codes mới cần thêm

| Code | HTTP | Mô tả |
|---|---|---|
| `NOT_TEAM_LEADER` | 422 | User không phải leader của team nào |
| `ASSIGNMENT_NOT_ACCEPTED` | 422 | Assignment còn ở trạng thái `Assigned`, chưa Accept |

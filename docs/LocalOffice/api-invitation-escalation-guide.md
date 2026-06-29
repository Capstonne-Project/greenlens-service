# Staff Invitation System & Report Escalation — API Guide

> **Tài liệu hướng dẫn tích hợp cho FE/Mobile**: Hệ thống mời nhân sự (invitation flow), reject re-queue, và LEO escalate report lên DEO.

---

## 1. Invitation Flow (BR-ORG-021)

### Tổng quan

Trước đây `RecruitStaff` thay đổi role ngay lập tức. Giờ chuyển sang **invitation flow**:

```
LEO gửi lời mời → Citizen nhận invitation → Accept / Decline
                                              ↓ Accept
                                    Role đổi + gán office + team
```

### 1.1 LEO gửi lời mời

```
POST /v1/offices/my/staff
Authorization: Bearer <LEO_TOKEN>
```

**Request:**
```json
{
  "email": "citizen@example.com",
  "targetRole": "Cleaner",
  "teamId": "3fa85f64-...",    // optional
  "isLeader": false             // optional
}
```

**Response 201:**
```json
{
  "isSuccess": true,
  "data": {
    "userId": "...",
    "email": "citizen@example.com",
    "fullName": "Nguyễn Văn A",
    "targetRole": "Cleaner",
    "localOfficeId": "...",
    "teamId": "3fa85f64-...",
    "teamMemberId": null          // ← null vì chưa accept
  }
}
```

> **Lưu ý:** Không thay đổi role ngay. Tạo `StaffInvitation` (7 ngày hết hạn).

**Error cases:**

| Status | Code | Khi nào |
|--------|------|---------|
| 404 | `USER_NOT_FOUND` | Email không tồn tại |
| 409 | `USER_ALREADY_IN_OFFICE` | User đã thuộc phường khác |
| 409 | `DUPLICATE_INVITATION` | Đã có invitation pending cho user này |
| 422 | `INVALID_ROLE_FOR_RECRUIT` | User không phải Citizen |
| 422 | `INVALID_ROLE_FOR_TEAM_MEMBER` | Role ↔ TeamType không khớp |

---

### 1.2 Citizen xem lời mời

```
GET /v1/invitations/my
Authorization: Bearer <CITIZEN_TOKEN>
```

**Response 200:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "invitationId": "...",
      "invitedByUserId": "...",
      "invitedByName": "Trần LEO",
      "targetRole": "Cleaner",
      "officeName": "Văn phòng MT P. Bến Nghé",
      "teamName": "Đội vệ sinh 1",
      "status": "Pending",
      "expiresAt": "2026-07-07T10:00:00Z",
      "createdAt": "2026-06-30T10:00:00Z"
    }
  ]
}
```

**Status values:** `Pending` | `Accepted` | `Declined` | `Expired` | `Cancelled`

> **FE lưu ý:** Nếu `status = Pending` nhưng `expiresAt < now` → hiển thị là "Hết hạn".

---

### 1.3 Citizen chấp nhận

```
POST /v1/invitations/{invitationId}/accept
Authorization: Bearer <CITIZEN_TOKEN>
```

**Response 200:**
```json
{
  "isSuccess": true,
  "data": {
    "userId": "...",
    "newRole": "Cleaner",
    "localOfficeId": "...",
    "teamId": "3fa85f64-..."
  }
}
```

> **Sau accept:** Role thay đổi ngay → FE cần refresh token hoặc logout/login lại để cập nhật claims.

**Error cases:**

| Status | Code | Khi nào |
|--------|------|---------|
| 404 | `INVITATION_NOT_FOUND` | ID không tồn tại |
| 403 | `FORBIDDEN` | Invitation không phải của bạn |
| 422 | `INVITATION_EXPIRED` | Quá 7 ngày |
| 422 | `INVITATION_ALREADY_RESPONDED` | Đã accept/decline trước đó |

---

### 1.4 Citizen từ chối

```
POST /v1/invitations/{invitationId}/decline
Authorization: Bearer <CITIZEN_TOKEN>
```

**Response 204:** No content.

---

### 1.5 LEO release nhân sự (sửa sai / nghỉ việc)

```
DELETE /v1/offices/my/staff/{userId}
Authorization: Bearer <LEO_TOKEN>
```

**Response 204:** No content.

**Hành vi:**
- Role → `Citizen`
- Xoá khỏi tất cả team memberships
- Clear `LocalOfficeId` và `DepartmentId`

**Error cases:**

| Status | Code | Khi nào |
|--------|------|---------|
| 404 | `USER_NOT_FOUND` | User không tồn tại |
| 403 | `USER_NOT_IN_YOUR_OFFICE` | User thuộc phường khác |
| 422 | `CANNOT_RELEASE_CITIZEN` | User đã là Citizen rồi |

---

## 2. Reject & Re-queue (BR-ORG-015)

### Luồng

```
Report (Submitted) → LEO reject → Status vẫn Submitted
                                 → AssignedOfficeId = null
                                 → Report quay lại Department queue
                                 → DEO hoặc hệ thống re-route
```

### API

```
POST /v1/reports/{id}/reject
Authorization: Bearer <LEO_TOKEN>
```

**Request:**
```json
{
  "reason": "Báo cáo trùng lặp với báo cáo #RP-20260630-001, cùng vị trí và thời điểm"
}
```

> **Ràng buộc:** `reason` ≥ 20 ký tự.

**Response 204:** No content.

**Khác biệt so với trước:**
- ❌ Trước: status → `Rejected` (terminal)
- ✅ Giờ: status giữ `Submitted`, clear `AssignedOfficeId` → quay lại hàng đợi Department

---

## 3. LEO Escalate to DEO (BR-ORG-016)

### Khi nào dùng?

LEO xác minh báo cáo xong, nhận thấy:
- Báo cáo thuộc **tuyến đường cấp TP** (Lê Lợi, Nguyễn Huệ...) — CITENCO quản lý
- Vấn đề vượt quá khả năng xử lý của phường
- Cần đơn vị cấp TP điều phối

### Luồng

```
Report (Verified/InProgress) → LEO bấm "Escalate"
                              → AssignedOfficeId = null
                              → Report xuất hiện trong DEO queue
                              → DEO dispatch cho CITENCO / đơn vị cấp TP
```

### API

```
POST /v1/reports/{id}/escalate
Authorization: Bearer <LEO_TOKEN>
```

**Request:**
```json
{
  "reason": "Tuyến đường Lê Lợi, thuộc CITENCO quản lý, phường không có thẩm quyền"
}
```

> **Ràng buộc:** `reason` ≥ 10 ký tự.

**Response 204:** No content.

**Preconditions:**
- Report phải ở trạng thái `Verified` hoặc `InProgress`
- LEO phải thuộc cùng office với report

**Error cases:**

| Status | Code | Khi nào |
|--------|------|---------|
| 404 | `REPORT_NOT_FOUND` | Report không tồn tại |
| 422 | `INVALID_STATUS_TRANSITION` | Report không ở Verified/InProgress |
| 403 | `OUTSIDE_JURISDICTION` | LEO không thuộc office của report |

---

## 4. Tổng hợp Endpoints

| Method | Route | Auth | Mô tả |
|--------|-------|------|-------|
| `POST` | `/v1/offices/my/staff` | LEO | Gửi invitation |
| `DELETE` | `/v1/offices/my/staff/{userId}` | LEO | Release staff → Citizen |
| `GET` | `/v1/invitations/my` | Any | Xem invitations của tôi |
| `POST` | `/v1/invitations/{id}/accept` | Citizen | Chấp nhận invitation |
| `POST` | `/v1/invitations/{id}/decline` | Citizen | Từ chối invitation |
| `POST` | `/v1/reports/{id}/reject` | LEO | Reject → re-queue |
| `POST` | `/v1/reports/{id}/escalate` | LEO | Escalate → DEO queue |

---

## 5. FE Integration Checklist

- [ ] **Citizen app**: Thêm tab/notification "Lời mời" — hiển thị Pending invitations
- [ ] **Citizen app**: Nút Accept / Decline trên mỗi invitation card
- [ ] **Citizen app**: Sau Accept → force refresh token (role đã đổi)
- [ ] **LEO dashboard**: Cập nhật "Tuyển nhân sự" → hiển thị là "Gửi lời mời" thay vì instant
- [ ] **LEO dashboard**: Thêm nút "Release" trên danh sách nhân sự
- [ ] **LEO dashboard**: Thêm nút "Escalate lên DEO" trên report detail (chỉ Verified/InProgress)
- [ ] **LEO dashboard**: Report bị reject → hiển thị rõ "Đã re-queue về hàng đợi Department"
- [ ] **DEO dashboard**: Hiển thị reports escalated trong queue (AssignedOfficeId = null)

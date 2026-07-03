# LEO Staff Management API

> **Version**: 1.1  
> **Updated**: 2026-06-03  
> **Module**: Organization → Staff Management  
> **Business Rules**: BR-ORG-005, BR-ORG-006

## Tổng quan

Cho phép **LEO** (Local Environmental Officer) quản lý toàn bộ nhân sự (Cleaner/Inspector) trong phường/xã:  
tuyển dụng, xem danh sách, thêm/xóa khỏi team, chuyển team.

### Flow tổng thể

```
                    ┌─────────────────────────────────┐
                    │  Citizen đăng ký tài khoản       │
                    │  (app/web, role = Citizen)       │
                    └──────────────┬──────────────────┘
                                   ↓
                    ┌──────────────────────────────────┐
                    │  LEO search email trên dashboard  │
                    │  POST /v1/offices/my/staff        │
                    │  ├─ Đổi role → Cleaner/Inspector  │
                    │  ├─ Gán LocalOfficeId             │
                    │  └─ (Optional) Gán vào team       │
                    └──────────────┬──────────────────┘
                                   ↓
              ┌────────────────────┴────────────────────┐
              ↓                                         ↓
    ┌─────────────────┐                       ┌─────────────────┐
    │ Đã có team       │                       │ Chưa có team     │
    │ → Nhận task ngay │                       │ → LEO thêm sau   │
    │ → Xem my-tasks   │                       │ POST /teams/     │
    └────────┬────────┘                       │  {id}/members    │
             │                                 └────────┬────────┘
             ↓                                          ↓
    ┌─────────────────────────────────────────────────────┐
    │            LEO quản lý nhân sự hàng ngày             │
    │  ┌───────────────┬────────────────┬──────────────┐  │
    │  │ Xem danh sách │ Chuyển team    │ Xóa khỏi team│  │
    │  │ GET /offices/ │ PUT /teams/    │ DELETE /teams/│  │
    │  │  my/staff     │  {id}/members/ │  {id}/members/│  │
    │  │               │  {uid}/transfer│  {uid}        │  │
    │  └───────────────┴────────────────┴──────────────┘  │
    └──────────────────────────────────────────────────────┘
```

---

## Bảng tổng hợp API

| #   | Method   | Endpoint                                       | Mô tả                                                 | Auth       |
| --- | -------- | ---------------------------------------------- | ----------------------------------------------------- | ---------- |
| 0   | `GET`    | `/v1/offices/my/staff/lookup?email=...`        | Tra cứu tài khoản Citizen trước khi recruit           | LEO, Admin |
| 1   | `POST`   | `/v1/offices/my/staff`                         | Tuyển Citizen → Cleaner/Inspector + gán office + team | LEO, Admin |
| 2   | `GET`    | `/v1/offices/my/staff`                         | Danh sách nhân sự trong phường                        | LEO, Admin |
| 3   | `POST`   | `/v1/teams/{teamId}/members`                   | Thêm nhân sự vào team                                 | LEO, Admin |
| 4   | `DELETE` | `/v1/teams/{teamId}/members/{userId}`          | Xóa nhân sự khỏi team (giữ role)                      | LEO, Admin |
| 5   | `PUT`    | `/v1/teams/{teamId}/members/{userId}/transfer` | Chuyển nhân sự sang team khác (atomic)                | LEO, Admin |

---

## Chi tiết API

### 0. Tra cứu tài khoản (Lookup)

```
GET /v1/offices/my/staff/lookup?email=nguyen.van.a@gmail.com
Authorization: Bearer <LEO or Admin token>
```

#### Query Parameters

| Param   | Type   | Required | Mô tả                                     |
| ------- | ------ | -------- | ----------------------------------------- |
| `email` | string | ✅       | Email chính xác của tài khoản cần tra cứu |

#### Response 200 OK

```json
{
  "code": "SUCCESS",
  "message": "Thành công.",
  "status": 200,
  "data": {
    "userId": "8a2e3b4c-...",
    "email": "nguyen.van.a@gmail.com",
    "fullName": "Nguyễn Văn A",
    "phoneNumber": "0901234567",
    "avatarUrl": null,
    "role": "Citizen",
    "isRecruitEligible": true,
    "ineligibleReason": null
  }
}
```

#### Khi không đủ điều kiện recruit

```json
{
  "data": {
    "userId": "...",
    "email": "da.co.role@gmail.com",
    "fullName": "Trần Thị B",
    "role": "Cleaner",
    "isRecruitEligible": false,
    "ineligibleReason": "Người dùng đã có vai trò Cleaner. Chỉ Citizen mới được recruit."
  }
}
```

#### Error Responses

| HTTP | Code             | Khi nào                            |
| ---- | ---------------- | ---------------------------------- |
| 404  | `USER_NOT_FOUND` | Email không tồn tại trong hệ thống |

> **FE Flow (auto-lookup khi nhập đúng email):**
>
> 1. LEO nhập email vào ô input
> 2. FE validate bằng regex email → chưa đúng format thì **không gọi API**
> 3. Khi email hợp lệ → **debounce 500ms** → tự động gọi `GET /v1/offices/my/staff/lookup?email=...`
> 4. Nếu 404 → hiện inline message "Không tìm thấy tài khoản với email này"
> 5. Nếu 200 + `isRecruitEligible=true` → hiện card preview (avatar, tên, email) + nút "Tuyển" ✅
> 6. Nếu 200 + `isRecruitEligible=false` → hiện card preview + badge lý do (`ineligibleReason`) + disable nút ❌
> 7. LEO sửa email → card biến mất, lặp lại từ bước 2
>
> **Gợi ý FE implementation:**
>
> ```javascript
> // Pseudo-code
> const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
>
> onEmailInput = debounce(async (email) => {
>   if (!EMAIL_REGEX.test(email)) return clearPreview();
>   const res = await fetch(`/v1/offices/my/staff/lookup?email=${email}`);
>   if (res.status === 404) showError("Không tìm thấy tài khoản");
>   else showPreviewCard(res.data);
> }, 500);
> ```

---

### 1. Tuyển nhân sự (Recruit)

```
POST /v1/offices/my/staff
Authorization: Bearer <LEO or Admin token>
```

#### Request Body

```json
{
  "email": "nguyen.van.a@gmail.com",
  "targetRole": "Cleaner",
  "teamId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isLeader": false
}
```

| Field        | Type   | Required | Mô tả                                                           |
| ------------ | ------ | -------- | --------------------------------------------------------------- |
| `email`      | string | ✅       | Email của tài khoản Citizen cần recruit                         |
| `targetRole` | enum   | ✅       | `Cleaner` hoặc `Inspector`                                      |
| `teamId`     | GUID   | ❌       | Nếu truyền → thêm vào team luôn. Team phải thuộc office của LEO |
| `isLeader`   | bool   | ❌       | Default `false`. Set `true` để gán làm Team Leader              |

#### Response 201 Created

```json
{
  "code": "SUCCESS",
  "message": "Thành công.",
  "status": 201,
  "data": {
    "userId": "8a2e3b4c-...",
    "email": "nguyen.van.a@gmail.com",
    "fullName": "Nguyễn Văn A",
    "assignedRole": "Cleaner",
    "localOfficeId": "1b2c3d4e-...",
    "teamId": "3fa85f64-...",
    "teamMemberId": "9f8e7d6c-..."
  }
}
```

#### Error Responses

| HTTP | Code                           | Khi nào                                                                |
| ---- | ------------------------------ | ---------------------------------------------------------------------- |
| 404  | `USER_NOT_FOUND`               | Email không tồn tại trong hệ thống                                     |
| 409  | `USER_ALREADY_IN_OFFICE`       | User đã thuộc phường/xã khác                                           |
| 409  | `USER_ALREADY_IN_TEAM`         | User đã là thành viên một đội                                          |
| 422  | `INVALID_ROLE_FOR_RECRUIT`     | User không phải Citizen (đã là DEO/LEO/Cleaner/...)                    |
| 422  | `INVALID_ROLE_FOR_TEAM_MEMBER` | TargetRole không phải Cleaner/Inspector, hoặc role không khớp TeamType |
| 422  | `TEAM_NOT_IN_OFFICE`           | Team không thuộc office của LEO                                        |
| 422  | `OFFICER_NO_OFFICE`            | LEO chưa được gán office                                               |

---

### 2. Danh sách nhân sự trong phường

```
GET /v1/offices/my/staff
Authorization: Bearer <LEO or Admin token>
```

#### Query Parameters

| Param      | Type   | Default | Mô tả                                                              |
| ---------- | ------ | ------- | ------------------------------------------------------------------ |
| `page`     | int    | 1       | Trang hiện tại                                                     |
| `pageSize` | int    | 20      | Số item mỗi trang                                                  |
| `search`   | string | —       | Tìm theo tên hoặc email (case-insensitive)                         |
| `role`     | enum   | —       | Lọc theo `Cleaner` hoặc `Inspector`                                |
| `hasTeam`  | bool   | —       | `true` = đã có team, `false` = chưa có team, không truyền = tất cả |

#### Response 200 OK

```json
{
  "code": "SUCCESS",
  "message": "Thành công.",
  "status": 200,
  "data": {
    "items": [
      {
        "userId": "8a2e3b4c-...",
        "fullName": "Nguyễn Văn A",
        "email": "nguyen.van.a@gmail.com",
        "phoneNumber": "0901234567",
        "avatarUrl": null,
        "role": "Cleaner",
        "teamId": "3fa85f64-...",
        "teamName": "Đội dọn dẹp Phường 1",
        "isLeader": false,
        "createdAt": "2026-05-20T10:00:00Z"
      },
      {
        "userId": "7b3d4e5f-...",
        "fullName": "Trần Thị B",
        "email": "tran.thi.b@gmail.com",
        "phoneNumber": null,
        "avatarUrl": null,
        "role": "Inspector",
        "teamId": null,
        "teamName": null,
        "isLeader": false,
        "createdAt": "2026-06-01T08:00:00Z"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "pageSize": 20,
      "totalCount": 2,
      "totalPages": 1,
      "hasPreviousPage": false,
      "hasNextPage": false
    }
  }
}
```

---

### 3. Thêm nhân sự vào team

```
POST /v1/teams/{teamId}/members
Authorization: Bearer <LEO or Admin token>
```

#### Request Body

```json
{
  "userId": "8a2e3b4c-1234-5678-abcd-ef0123456789",
  "isLeader": false
}
```

| Field      | Type | Required | Mô tả                                              |
| ---------- | ---- | -------- | -------------------------------------------------- |
| `userId`   | GUID | ✅       | ID của user cần thêm vào team                      |
| `isLeader` | bool | ❌       | Default `false`. Set `true` để gán làm Team Leader |

#### Response 201 Created

```json
{
  "code": "SUCCESS",
  "message": "Thành công.",
  "status": 201,
  "data": {
    "id": "aaa-bbb-...",
    "teamId": "3fa85f64-...",
    "userId": "8a2e3b4c-...",
    "isLeader": false
  }
}
```

#### Error Responses

| HTTP | Code                           | Khi nào                                                          |
| ---- | ------------------------------ | ---------------------------------------------------------------- |
| 404  | `TEAM_NOT_FOUND`               | Team không tồn tại                                               |
| 404  | `USER_NOT_FOUND`               | User không tồn tại                                               |
| 409  | `MEMBER_ALREADY_IN_TEAM`       | User đã là thành viên của team này                               |
| 422  | `INVALID_ROLE_FOR_TEAM_MEMBER` | Role không khớp TeamType (Cleaner↔Cleanup, Inspector↔Inspection) |

> **Lưu ý:** User phải đã có role Cleaner/Inspector (thường qua recruit trước) để thêm vào team.

---

### 4. Xóa nhân sự khỏi team

```
DELETE /v1/teams/{teamId}/members/{userId}
Authorization: Bearer <LEO or Admin token>
```

#### Response 204 No Content

```json
{
  "code": "SUCCESS",
  "message": "Đã xóa thành viên khỏi team.",
  "status": 200
}
```

#### Error Responses

| HTTP | Code               | Khi nào                                 |
| ---- | ------------------ | --------------------------------------- |
| 404  | `MEMBER_NOT_FOUND` | User không phải thành viên của team này |

> **⚠️ Quan trọng:**
>
> - **Xóa khỏi team ≠ đuổi khỏi phường** — User vẫn giữ role (Cleaner/Inspector) và vẫn thuộc LocalOffice.
> - LEO vẫn thấy user trong `GET /v1/offices/my/staff` với `hasTeam=false`.
> - LEO có thể thêm lại vào team khác bất cứ lúc nào.

---

### 5. Chuyển nhân sự sang team khác (Transfer)

```
PUT /v1/teams/{teamId}/members/{userId}/transfer
Authorization: Bearer <LEO or Admin token>
```

#### Path Parameters

| Param    | Mô tả                         |
| -------- | ----------------------------- |
| `teamId` | ID team hiện tại (team nguồn) |
| `userId` | ID user cần chuyển            |

#### Request Body

```json
{
  "newTeamId": "5fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isLeader": false
}
```

| Field       | Type | Required | Mô tả                                                      |
| ----------- | ---- | -------- | ---------------------------------------------------------- |
| `newTeamId` | GUID | ✅       | Team đích để chuyển sang                                   |
| `isLeader`  | bool | ❌       | Default `false`. Set `true` nếu chuyển làm leader team mới |

#### Response 200 OK

```json
{
  "code": "SUCCESS",
  "message": "Thành công.",
  "status": 200,
  "data": {
    "userId": "8a2e3b4c-...",
    "oldTeamId": "3fa85f64-...",
    "newTeamId": "5fa85f64-...",
    "newTeamMemberId": "abc123-...",
    "isLeader": false
  }
}
```

#### Error Responses

| HTTP | Code                           | Khi nào                                   |
| ---- | ------------------------------ | ----------------------------------------- |
| 404  | `TEAM_NOT_FOUND`               | Team nguồn hoặc team đích không tồn tại   |
| 404  | `MEMBER_NOT_IN_TEAM`           | User không phải thành viên của team nguồn |
| 409  | `MEMBER_ALREADY_IN_TEAM`       | User đã ở team đích rồi                   |
| 422  | `TRANSFER_SAME_TEAM`           | Team đích trùng team nguồn                |
| 422  | `TEAM_NOT_IN_OFFICE`           | Team không thuộc office của LEO           |
| 422  | `INVALID_ROLE_FOR_TEAM_MEMBER` | Role không khớp TeamType mới              |
| 422  | `OFFICER_NO_OFFICE`            | LEO chưa được gán office                  |

> **Đặc điểm:**
>
> - **Atomic:** Remove khỏi team cũ + Add vào team mới trong 1 transaction — không có trạng thái treo.
> - **Role không đổi:** Chuyển team chỉ thay đổi team membership, không ảnh hưởng role.

---

## Business Rules

| ID          | Rule                | Mô tả                                                                     |
| ----------- | ------------------- | ------------------------------------------------------------------------- |
| BR-ORG-005a | Chỉ recruit Citizen | User phải có role = Citizen. DEO, LEO, Admin, Cleaner, Inspector → reject |
| BR-ORG-005b | 1 user → 1 office   | User chỉ thuộc 1 phường/xã. Nếu đã có LocalOfficeId → reject              |
| BR-ORG-005c | 1 user → 1 team     | Mỗi user chỉ thuộc 1 team tại 1 thời điểm                                 |
| BR-ORG-005d | Role khớp TeamType  | Cleaner → Cleanup team. Inspector → Inspection team                       |
| BR-ORG-005e | Team thuộc office   | Team.LocalOfficeId phải = LEO's LocalOfficeId                             |
| BR-ORG-005f | TeamId optional     | Có thể recruit vào phường mà chưa gán team                                |
| BR-ORG-006a | Remove giữ role     | Xóa khỏi team không đổi role, user vẫn thuộc phường                       |
| BR-ORG-006b | Transfer atomic     | Chuyển team = remove + add trong 1 transaction                            |

---

## Tổng hợp Error Codes

| Code                           | HTTP | Mô tả                                  |
| ------------------------------ | ---- | -------------------------------------- |
| `USER_NOT_FOUND`               | 404  | Email/User không tồn tại               |
| `TEAM_NOT_FOUND`               | 404  | Team không tồn tại                     |
| `MEMBER_NOT_FOUND`             | 404  | Thành viên không tồn tại trong team    |
| `MEMBER_NOT_IN_TEAM`           | 404  | User không thuộc team nguồn (transfer) |
| `USER_ALREADY_IN_OFFICE`       | 409  | User đã thuộc phường khác              |
| `USER_ALREADY_IN_TEAM`         | 409  | User đã có team (recruit)              |
| `MEMBER_ALREADY_IN_TEAM`       | 409  | User đã trong team (add/transfer)      |
| `INVALID_ROLE_FOR_RECRUIT`     | 422  | User không phải Citizen                |
| `INVALID_ROLE_FOR_TEAM_MEMBER` | 422  | Role không khớp TeamType               |
| `TEAM_NOT_IN_OFFICE`           | 422  | Team không thuộc office của LEO        |
| `TRANSFER_SAME_TEAM`           | 422  | Chuyển về chính team hiện tại          |
| `OFFICER_NO_OFFICE`            | 422  | LEO chưa được gán office               |

---

## Kịch bản FE

### Tuyển nhân sự mới

1. LEO mở trang "Quản lý nhân sự"
2. Nhấn "Tuyển mới" → nhập email
3. Chọn role (Cleaner / Inspector)
4. (Optional) Chọn team từ dropdown
5. Bấm "Tuyển" → `POST /v1/offices/my/staff`
6. Thành công → refresh danh sách

### Xem danh sách nhân sự

1. `GET /v1/offices/my/staff` → hiển thị bảng
2. Filter tabs: Tất cả | Cleaner | Inspector
3. Filter: Có team / Chưa có team
4. Search box: tìm theo tên, email
5. Cột "Team": hiển thị tên team hoặc badge "Chưa có team"

### Thêm nhân sự chưa có team vào team

1. Từ danh sách nhân sự → lọc `hasTeam=false`
2. Chọn user → chọn team → `POST /v1/teams/{teamId}/members`

### Chuyển nhân sự sang team khác

1. LEO mở chi tiết team → thấy danh sách thành viên
2. Nhấn icon "Chuyển" bên cạnh member
3. Chọn team đích từ dropdown (chỉ hiện team cùng office, cùng type)
4. Bấm "Chuyển" → `PUT /v1/teams/{teamId}/members/{userId}/transfer`

### Xóa nhân sự khỏi team

1. LEO mở chi tiết team → danh sách thành viên
2. Nhấn icon "Xóa" bên cạnh member
3. Confirm dialog: "Xóa sẽ chỉ bỏ khỏi team, không đuổi khỏi phường"
4. Bấm "Xác nhận" → `DELETE /v1/teams/{teamId}/members/{userId}`
5. User chuyển sang trạng thái "Chưa có team" trong danh sách nhân sự

---

## Liên quan

- [Team CRUD APIs](api-team-workflow.md) — Tạo/sửa/xóa team, quản lý thành viên
- [Cleanup Team Flow](api-cleanup-team-flow.md) — Workflow Cleaner nhận task
- [FE Team Workflow Guide](fe-team-workflow-guide.md) — Hướng dẫn FE chi tiết
- [SEED_ACCOUNTS.md](SEED_ACCOUNTS.md) — Danh sách tài khoản test

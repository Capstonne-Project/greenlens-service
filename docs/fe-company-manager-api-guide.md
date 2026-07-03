# Company Manager — API Guide (Mobile / FE)

> **Prefix:** `/v1` · **Envelope:** `{ code, message, status, data }`  
> **Roles:** `CompanyManager` (dashboard), `CompanyStaff` (field — dùng `TeamsController`)  
> **Seed QA:** `company@greenlens.dev` / `Lualua123@` — xem [`SEED_ACCOUNTS.md`](./SEED_ACCOUNTS.md)

---

## Pagination chuẩn

Mọi list dùng object `pagination`:

```json
{
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 42,
    "totalPages": 3,
    "hasNext": true,
    "hasPrev": false
  }
}
```

---

## 1. Công ty của tôi — `GET /v1/companies/my`

**Auth:** `CompanyManager`

**Response `data`:** `CompanyDetailResponse` (id, name, contractNumber, status, serviceAreas[], staffCount, …)

---

## 2. Queue chờ phân công — `GET /v1/reports/company-queue`

**Query:** `page`, `pageSize`, `severity?`

**Response `data`:**

```json
{
  "items": [
    {
      "reportId": "uuid",
      "code": "REP-MOB-CQ001",
      "address": "123 Nguyễn Huệ, Phường 1, TP.HCM",
      "wardCode": "27145",
      "latitude": 10.7769,
      "longitude": 106.7009,
      "categoryName": "Ô nhiễm rác thải",
      "severity": "Medium",
      "dispatchedAt": "2026-06-01T10:00:00Z",
      "slaResolveDueAt": "2026-06-08T10:00:00Z"
    }
  ],
  "pagination": { "...": "..." }
}
```

**Điều kiện:** `report.status === Verified` và `assignedCompanyId === company của CM`.

---

## 3. Phân công team — `POST /v1/reports/{id}/assign-company-team`

> ⚠️ **KHÔNG** dùng `{ teamId }` đơn lẻ. BE expect **mảng teams**.

**Body:**

```json
{
  "teams": [
    {
      "teamId": "uuid-cua-company-team",
      "note": "Ưu tiên sáng sớm"
    }
  ]
}
```

**Response:** 200, `data: null`  
**Transition:** `Verified` → `InProgress`

**Lỗi thường gặp:** `REPORT_NOT_DISPATCHED_TO_YOUR_COMPANY`, `TEAM_WORKLOAD_EXCEEDED`, `AT_LEAST_ONE_TEAM`

---

## 4. Task đã phân công — `GET /v1/reports/company-assignments`

**Query:** `page`, `pageSize`, `status?` (AssignmentStatus), `reportStatus?`, `search?`

**Item shape:**

```json
{
  "assignmentId": "uuid",
  "assignmentStatus": "InProgress",
  "assignedAt": "...",
  "startedAt": "...",
  "completedAt": null,
  "progressPercent": 40,
  "progressNote": "Đang thu gom",
  "progressUpdatedAt": "...",
  "note": "Mobile demo assignment",
  "report": {
    "reportId": "uuid",
    "code": "REP-MOB-TSK001",
    "address": "...",
    "wardCode": "27145",
    "categoryName": "Ô nhiễm rác thải",
    "severity": "Medium",
    "status": "InProgress",
    "slaResolveDueAt": "..."
  },
  "team": {
    "teamId": "uuid",
    "teamName": "Đội công ty Mobile Demo",
    "memberCount": 1
  },
  "assignedByName": "Mobile Company Manager"
}
```

---

## 5. Chi tiết tiến độ — `GET /v1/reports/company-assignments/{reportId}`

Trả assignment + progress board cho report thuộc company (CM only).

---

## 6–11. Team CRUD — `/v1/teams/company-teams`

| Method | Path | Body / ghi chú |
|--------|------|----------------|
| GET | `/v1/teams/company-teams` | List team công ty |
| POST | `/v1/teams/company-teams` | `{ "name": "Đội A", "teamType": "Cleanup" }` |
| PUT | `/v1/teams/company-teams/{id}` | `{ "name": "Tên mới" }` |
| DELETE | `/v1/teams/company-teams/{id}` | Soft deactivate |
| POST | `/v1/teams/company-teams/{teamId}/members` | `{ "userId": "uuid", "isLeader": false }` |
| DELETE | `/v1/teams/company-teams/{teamId}/members/{userId}` | — |

`teamType` cho company team: chỉ `Cleanup` (Inspection không thuộc công ty).

---

## 12–14. Nhân viên — `/v1/companies/my/staff`

### GET `/v1/companies/my/staff`

Query: `page`, `pageSize`, `isActive?`

### POST `/v1/companies/my/staff`

**Body:**

```json
{
  "email": "staff.new@company.dev",
  "fullName": "Nguyen Van B",
  "position": "Thu gom",
  "teamId": "uuid-optional"
}
```

**Response 201 `data`:**

```json
{
  "userId": "uuid",
  "email": "staff.new@company.dev",
  "fullName": "Nguyen Van B",
  "tempPassword": "Xy9!kL2mNp",
  "companyId": "uuid",
  "position": "Thu gom",
  "teamId": "uuid-or-null"
}
```

> `tempPassword` **chỉ trả 1 lần** — CM copy gửi nhân viên. Staff login → `mustChangePassword: true` → đổi MK.

### PUT `/v1/companies/my/staff/{userId}/status`

```json
{ "isActive": false }
```

---

## CompanyStaff field worker (shared với Cleaner)

Dùng **`TeamsController`** — không qua CompaniesController:

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/v1/teams/my-tasks` | Queue task |
| PUT | `/v1/teams/my-tasks/{assignmentId}/accept` | Leader accept |
| PUT | `/v1/teams/my-tasks/{assignmentId}/decline` | Leader decline |
| PUT | `/v1/reports/{reportId}/progress` | multipart — xem [`cleaner-progress-resolve-fe-guide.md`](./cleaner-progress-resolve-fe-guide.md) |
| PUT | `/v1/reports/{reportId}/resolve` | JSON after images |

**Seed staff leader:** `staff@greenlens.dev` / `Lualua123@` — report `REP-MOB-TSK001`.

---

## Profile user

`GET /v1/users/profile` — **không** có `/users/me`.

Login `data.user.role`: `CompanyManager` | `CompanyStaff` (string enum, PascalCase).

JWT **không** embed `teamId` / `companyId` — resolve qua API task/company-my khi cần.

# FE Guide — Idempotency-Key (chống double-submit)

> **Phiên bản:** 2026-08-01 · **Backend:** GreenLens API v1 · **Liên quan:** BR-REP-010, BR-SYS-004

Tài liệu hướng dẫn **Mobile** và **Web** tích hợp header `Idempotency-Key` để tránh tạo bản ghi trùng khi user bấm Submit nhiều lần hoặc mạng timeout/retry.

---

## 1. Tóm tắt — FE phải làm gì

| Việc | Bắt buộc? | Ghi chú |
|------|-----------|---------|
| Sinh **một UUID** khi user bắt đầu thao tác (bấm Submit) | **Có** (endpoint trong bảng §4) | Không sinh key mới khi auto-retry |
| Gửi header `Idempotency-Key: {uuid}` trên mọi request (kể cả retry) | **Có** | Cùng payload body |
| **Disable nút** + loading khi đang gửi | **Có** (UX) | Idempotency là lớp bảo vệ server, không thay UI |
| Coi response **replay** (2xx, cùng data) như **thành công** | **Có** | Navigate / toast success như lần đầu |
| Xử lý `409 IDEMPOTENCY_IN_PROGRESS` — chờ 1–3s, retry cùng key | **Có** | Không sinh key mới |
| Xử lý `422 IDEMPOTENCY_KEY_REUSED` — sinh key mới | Có (edge case) | Xảy ra khi đổi body nhưng giữ key cũ |

**Phase 1:** Header **optional** — app cũ không gửi vẫn hoạt động. **Khuyến nghị ship Mobile/Web càng sớm càng tốt** cho các endpoint P0/P1.

---

## 2. Cách hoạt động (client ↔ server)

```
User bấm "Gửi báo cáo"
  → FE sinh key = uuid()  (lưu trong state màn hình / session submit)
  → POST /v1/reports + Idempotency-Key + body
       ├─ Lần 1: server xử lý → 201 + reportId → cache 24h
       ├─ Retry (timeout): cùng key + cùng body → 201 replay (cùng reportId)
       ├─ Double-tap song song: request 2 → 409 IN_PROGRESS → retry sau 2s
       └─ Đổi body giữ key cũ → 422 KEY_REUSED → FE sinh key mới
```

**Quan trọng:**

- Key gắn với **user đăng nhập** (hoặc IP nếu anonymous) + **method** + **route** + **key client**.
- User A không replay được key của user B.
- **Body phải giống hệt** (JSON serialized) giữa các lần gửi cùng key.

---

## 3. Header & code mẫu

### 3.1 HTTP

```http
POST /v1/reports HTTP/1.1
Authorization: Bearer eyJ...
Content-Type: application/json
Idempotency-Key: 7c9e6679-7425-40de-944b-e07fc1f90ae7

{ "categoryId": "...", "latitude": 10.76, ... }
```

### 3.2 TypeScript / Axios interceptor (gợi ý)

```typescript
import { v4 as uuidv4 } from 'uuid';

/** Gọi một lần khi user confirm submit — giữ key đến khi success hoặc user hủy. */
export function beginIdempotentAction(): string {
  return uuidv4();
}

export async function postIdempotent<T>(
  url: string,
  body: unknown,
  idempotencyKey: string,
  config?: AxiosRequestConfig,
): Promise<T> {
  const maxAttempts = 3;
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    try {
      const res = await api.post(url, body, {
        ...config,
        headers: {
          ...config?.headers,
          'Idempotency-Key': idempotencyKey,
        },
      });
      return res.data;
    } catch (err) {
      const code = err.response?.data?.code;
      if (code === 'IDEMPOTENCY_IN_PROGRESS' && attempt < maxAttempts - 1) {
        await sleep(2000);
        continue;
      }
      throw err;
    }
  }
  throw new Error('Idempotency retry exhausted');
}
```

### 3.3 React Native / Flutter

- **React Native:** `import 'react-native-get-random-values'` + `uuid` package.
- **Flutter:** `import 'package:uuid/uuid.dart';` → `Uuid().v4()`.
- Lưu key trong state widget form (`useRef` / `StatefulWidget`) — **không** lưu AsyncStorage trừ khi cần survive app kill (submit báo cáo: thường giữ trong memory đủ).

---

## 4. Bảng endpoint có Idempotency-Key

Base URL: `/v1`. Cột **App** = nơi **bắt buộc tích hợp** trước.

### P0 — Ưu tiên cao (ship trước)

| ID | ☐ Mobile | ☐ Web | Method | Endpoint | Actor | TTL | Ghi chú FE |
|----|----------|-------|--------|----------|-------|-----|------------|
| **IDM-01** | ☐ | — | POST | `/reports` | Citizen | 24h | **Case chính** — gửi báo cáo; replay trả cùng `reportId`; không tốn quota 5/h |
| **IDM-02** | ☐ | ☐ | POST | `/auth/register` | Citizen | 1h | Double-tap đăng ký → replay 201 thay vì `EMAIL_TAKEN` |
| **IDM-03** | ☐ | — | POST | `/auth/google-login` | Citizen | 1h | Tránh side-effect session lặp |

### P1 — Citizen / field Mobile

| ID | ☐ Mobile | ☐ Web | Method | Endpoint | Actor | Ghi chú FE |
|----|----------|-------|--------|----------|-------|------------|
| **IDM-04** | ☐ | — | POST | `/reports/{id}/rate` | Citizen | Replay success thay vì `AlreadyRated` |
| **IDM-05** | ☐ | — | PUT | `/reports/{id}/close` | Citizen | Replay thay vì invalid transition |
| **IDM-06** | ☐ | — | POST | `/reports/{id}/reopen-requests` | Citizen | Race khi gửi yêu cầu mở lại |
| **IDM-07** | ☐ | ☐ | POST | `/reports/{reportId}/comments` | Mọi role được comment | Tránh comment trùng |
| **IDM-08** | ☐ | — | POST | `/community-cleanups/{eventId}/join` | Citizen | Replay thay vì `AlreadyJoined` |
| **IDM-09** | ☐ | — | POST | `/community-cleanups/{eventId}/check-in` | Citizen/Leader | GPS check-in trùng |
| **IDM-10** | ☐ | — | POST | `/invitations/{invitationId}/accept` | Citizen | **Critical** — đổi role Cleaner/Inspector |
| **IDM-11** | ☐ | — | PUT | `/teams/my-tasks/{reportId}/accept` | Cleaner/CompanyStaff/Inspector | Nhận task |
| **IDM-12** | ☐ | — | POST | `/teams/my-tasks/{reportId}/check-in` | Cleaner/CompanyStaff | BR-CLN-002 GPS |
| **IDM-13** | ☐ | — | PUT | `/reports/{id}/resolve` | Cleaner (leader) | Hoàn thành + ảnh after |
| **IDM-14** | ☐ | — | POST | `/inspections/{id}/accept` | Inspector | Nhận hồ sơ điều tra |
| **IDM-15** | ☐ | — | POST | `/inspections/{id}/confirm-arrival` | Inspector | Thay cho `check-in` deprecated |
| **IDM-16** | ☐ | — | PUT | `/inspections/{id}/submit-field-report` | Inspector | Nộp biên bản nặng — retry an toàn |
| **IDM-17** | ☐ | ☐ | POST | `/auth/request-otp` | Tất cả | Giảm spam OTP queue khi double-tap |

> **IDM-15:** `POST /inspections/{id}/check-in` đã **deprecated** — dùng `confirm-arrival`.

### P1 — Web officer (LEO / DEO)

| ID | ☐ Mobile | ☐ Web | Method | Endpoint | Actor | Ghi chú FE |
|----|----------|-------|--------|----------|-------|------------|
| **IDM-18** | — | ☐ | PUT | `/reports/{id}/verify` | LEO | Verify + notification |
| **IDM-19** | — | ☐ | POST | `/reports/{id}/assign` | LEO | Giao việc team |
| **IDM-20** | — | ☐ | POST | `/reports/{id}/dispatch-to-company` | LEO/DEO | Dispatch công ty |
| **IDM-21** | — | ☐ | POST | `/reports/{id}/assign-company-team` | CompanyManager | Giao team công ty |
| **IDM-22** | — | ☐ | POST | `/reports/{id}/inspections` | LEO | Tạo hồ sơ inspection |

### Không dùng Idempotency-Key (tham khảo)

| Nhóm | Ví dụ | Lý do |
|------|-------|-------|
| GET | `/reports/my`, map, catalog | Safe method |
| Toggle | `POST …/comments/{id}/like` | Idempotent by design |
| Auth đặc biệt | `verify-otp`, `refresh-token`, `forgot-password` | Semantics khác |
| DELETE | xóa draft, comment | Lặp → 404 OK |

---

## 5. Mã lỗi & xử lý UI

| HTTP | `code` | FE xử lý |
|------|--------|----------|
| 409 | `IDEMPOTENCY_IN_PROGRESS` | Chờ 1–3 giây, **retry cùng key** (tối đa 2–3 lần). Không báo lỗi cho user nếu retry thành công. |
| 422 | `IDEMPOTENCY_KEY_REUSED` | Body đã đổi — **sinh key mới** và gửi lại (hoặc báo lỗi dev nếu bug client). |
| 422 | `IDEMPOTENCY_KEY_REQUIRED` | (Phase 2) Bắt buộc gửi header — hiện chưa bật. |
| 422 | `IDEMPOTENCY_KEY_INVALID` | Key > 128 ký tự — sinh UUID chuẩn. |
| 2xx | `SUCCESS` | **Replay hoặc lần đầu** — xử lý giống nhau (navigate, toast success). |

**Không** hiển thị “lỗi trùng lặp” khi replay trả 200/201 với data hợp lệ.

---

## 6. UX khuyến nghị theo màn hình

### 6.1 Gửi báo cáo (IDM-01) — Mobile Citizen

1. User bấm **Gửi báo cáo** → sinh `idempotencyKey`, disable nút.
2. Timeout client **≥ 60s** (upload R2 + AI có thể lâu).
3. Nếu timeout mạng nhưng không chắc server đã nhận → **retry cùng key** (không tạo báo cáo mới).
4. Success (201 hoặc replay 201) → điều hướng chi tiết báo cáo với `data.reportId`.
5. Validation 422 (GPS, profanity, …) → **enable nút**, user sửa form; có thể **giữ key** nếu body không đổi, hoặc sinh key mới sau khi sửa.

### 6.2 Đăng ký (IDM-02)

- Sinh key khi bấm **Đăng ký**.
- Replay 201 → chuyển màn OTP như bình thường (xem `fe-async-notifications-auth-otp-guide.md`).

### 6.3 LEO Verify / Assign (IDM-18..22) — Web

- Sinh key khi confirm dialog “Xác minh” / “Phân công”.
- Replay → đóng dialog, refresh queue (data giống lần đầu).

### 6.4 Rate / Close / Reopen (IDM-04..06)

- Một key cho mỗi lần user bấm submit trên form đánh giá / đóng / mở lại.
- Replay tránh hiển thị `AlreadyRated` hoặc `InvalidStatusTransition`.

---

## 7. Mapping checklist API coverage

| Checklist ID | Endpoint | Idempotency ID |
|--------------|----------|----------------|
| CIT-03 | POST `/v1/reports` | IDM-01 |
| CIT-11 | PUT `/v1/reports/{id}/close` | IDM-05 |
| CIT-12 | POST `/v1/reports/{id}/reopen-requests` | IDM-06 |
| CIT-13 | POST `/v1/reports/{id}/rate` | IDM-04 |
| CIT-17 | POST `/v1/reports/{reportId}/comments` | IDM-07 |
| CIT-29 | POST `/v1/community-cleanups/{eventId}/join` | IDM-08 |
| CIT-32 | POST `/v1/community-cleanups/{eventId}/check-in` | IDM-09 |
| CIT-34 | POST `/v1/invitations/{id}/accept` | IDM-10 |
| CLN-05 | PUT `/v1/teams/my-tasks/{reportId}/accept` | IDM-11 |
| CLN-07 | POST `/v1/teams/my-tasks/{reportId}/check-in` | IDM-12 |
| CLN-15 | PUT `/v1/reports/{id}/resolve` | IDM-13 |
| INS-03 | POST `/v1/inspections/{id}/accept` | IDM-14 |
| INS-05 | POST `/v1/inspections/{id}/confirm-arrival` | IDM-15 |
| INS-07 | PUT `/v1/inspections/{id}/submit-field-report` | IDM-16 |
| AUTH-01 | POST `/v1/auth/register` | IDM-02 |
| AUTH-03 | POST `/v1/auth/google-login` | IDM-03 |
| AUTH-04 | POST `/v1/auth/request-otp` | IDM-17 |

---

## 8. FAQ

**Q: Có cần Idempotency-Key cho GET không?**  
A: Không.

**Q: Retry sau 422 validation có dùng lại key không?**  
A: Nếu **đã sửa body** → nên sinh key mới. Nếu body không đổi (retry mạng) → giữ key.

**Q: Key có hết hạn không?**  
A: Có — 24h (báo cáo/workflow), 1h (auth). Sau TTL, key mới được coi là request mới.

**Q: Idempotency thay duplicate detection (BR-REP-030)?**  
A: **Không.** Idempotency chống **cùng intent** (double-tap). Duplicate geo/AI vẫn chạy cho báo cáo **khác key**.

**Q: Web chưa gửi header có lỗi không?**  
A: Không — Phase 1 optional. Mobile nên ship trước cho P0.

---

## 9. Checklist triển khai FE

- [ ] Axios/fetch wrapper hỗ trợ `Idempotency-Key`
- [ ] P0 Mobile: `POST /reports`, register, google-login
- [ ] P1 Mobile: rate, close, reopen, comments, cleanup, invitations, tasks, inspections
- [ ] P1 Web: verify, assign, dispatch, inspections create
- [ ] Xử lý `IDEMPOTENCY_IN_PROGRESS` với backoff
- [ ] QA: double-tap Submit báo cáo → chỉ 1 row DB / 1 reportId
- [ ] QA: airplane mode retry → cùng reportId

---

**Tham chiếu backend:** `00_API_CONVENTIONS.md` §9 · `SupportsIdempotencyAttribute` trên controllers.

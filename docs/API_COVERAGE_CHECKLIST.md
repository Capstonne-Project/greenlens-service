# GreenLens — API Coverage Checklist (FE Web & Mobile)

> **Mục đích:** Đối chiếu repo **FE Web** và **Mobile** — tick từng API theo **luồng nghiệp vụ** của từng actor (không chỉ liệt kê phẳng theo endpoint).  
> **Nguồn:** Quét trực tiếp `src/Greenlens.Api/Controllers/` · branch `develop` · cập nhật **2026-08-03**.  
> **Base URL API:** `/v1` · **SignalR:** `/hubs/notifications`

---

## Cách dùng

1. Mỗi **luồng** = một quy trình end-to-end (vd. LEO **Cleanup** vs **Inspection** là 2 nhánh song song trên cùng Report).
2. Cột **Bước** = thứ tự gọi API trong luồng (cùng bước = song song / tuỳ chọn).
3. Cột **☐ Web** / **☐ Mobile** — tick (`x`) khi đã tìm thấy HTTP call trong repo FE tương ứng.
4. **`—`** = platform không dùng endpoint (theo actor → device).
5. **`⚠️`** = deprecated — không integrate mới.
6. Luồng **Chung** (§1–3): mọi actor đều cần — không lặp lại trong từng actor.
7. Tìm nhanh: `rg "/v1/reports/queue"` trong repo FE.

### Actor → Platform

| Actor | App | Repo cần kiểm |
|-------|-----|---------------|
| **Citizen** | Mobile | Mobile |
| **Cleaner** | Mobile | Mobile |
| **Inspector** | Mobile | Mobile |
| **CompanyStaff** | Mobile | Mobile |
| **LEO** | Web | FE Web |
| **DEO** | Web | FE Web |
| **CompanyManager** | Web | FE Web |
| **Admin** | Web | FE Web |

### Thống kê nhanh (ước lượng)

| Nhóm | Số endpoint |
|------|-------------|
| Public / Catalog / Map | 5 |
| Auth & Profile (mọi user) | ~22 |
| Citizen (Mobile) | ~45 |
| Cleaner (Mobile) | ~35 |
| Inspector (Mobile) | ~25 |
| CompanyStaff (Mobile) | ~30 |
| LEO (Web) | ~55 |
| DEO (Web) | ~35 |
| CompanyManager (Web) | ~30 |
| Admin (Web) | ~60 |

---

## Mục lục

### Chung (mọi actor)

1. [Public & Catalog](#1-public--catalog)
2. [Auth & Session](#2-auth--session-mọi-actor)
3. [Profile, Media, Notifications](#3-profile-media-notifications-mọi-actor)

### Theo actor — phân luồng

4. [Citizen — Mobile](#4-citizen--mobile)
   - [4.A Gửi báo cáo](#4a-luồng-gửi-báo-cáo) · [4.B Theo dõi](#4b-luồng-theo-dõi-báo-cáo) · [4.C Sau xử lý](#4c-luồng-sau-xử-lý-resolved) · [4.D Cộng đồng](#4d-luồng-bình-luận--gamification) · [4.E Volunteer cleanup](#4e-luồng-community-cleanup-volunteer) · [4.F Lời mời team](#4f-luồng-lời-mời-đội-cộng-đồng)
5. [Cleaner — Mobile](#5-cleaner--mobile)
   - [5.A Cleanup task](#5a-luồng-thực-hiện-task-dọn-dẹp) · [5.B Community leader](#5b-luồng-dẫn-dắt-community-cleanup)
6. [Inspector — Mobile](#6-inspector--mobile)
   - [6.A Inspection hiện trường](#6a-luồng-inspection-hiện-trường) · [6.B Violating entity](#6b-luồng-quản-lý-violating-entity) · [6.C Nhận/từ chối task](#6c-luồng-nhậntừ-chối-task-team)
7. [CompanyStaff — Mobile](#7-companystaff--mobile)
   - [7.A Cleanup task công ty](#7a-luồng-thực-hiện-task-công-ty)
8. [LEO — Web](#8-leo--web)
   - [8.A Xác minh & triage](#8a-luồng-xác-minh--triage) · [8.B Cleanup](#8b-luồng-dọn-dẹp-cleanup-track) · [8.C Inspection](#8c-luồng-xử-phạt-inspection-track) · [8.D Duplicate](#8d-luồng-phát-hiện-trùng) · [8.E Tái phát](#8e-luồng-nghi-tái-phạm) · [8.F Reopen](#8f-luồng-citizen-reopen) · [8.G Community cleanup](#8g-luồng-duyệt-community-cleanup) · [8.H Team & staff](#8h-luồng-quản-lý-team--staff) · [8.I KPI & export](#8i-luồng-kpi--báo-cáo)
9. [DEO — Web](#9-deo--web)
   - [9.A Org sở & phường](#9a-luồng-quản-trị-sở--phường) · [9.B Công ty đối tác](#9b-luồng-quản-lý-công-ty-đối-tác) · [9.C Giám sát báo cáo](#9c-luồng-giám-sát-báo-cáo--team-scope-tỉnh)
10. [CompanyManager — Web](#10-companymanager--web)
    - [10.A Công ty & nhân sự](#10a-luồng-quản-lý-công-ty--nhân-sự) · [10.B Teams](#10b-luồng-quản-lý-team-công-ty) · [10.C Phân công task](#10c-luồng-phân-công--theo-dõi-task-cleanup) · [10.D Dashboard](#10d-luồng-dashboard-analytics)
11. [Admin — Web](#11-admin--web)
    - [11.A Users & roles](#11a-luồng-quản-lý-user--role) · [11.B Moderation](#11b-luồng-kiểm-duyệt-báo-cáo) · [11.C Catalog](#11c-luồng-cấu-hình-danh-mục) · [11.D RBAC & config](#11d-luồng-rbac--penalty--gamification) · [11.E Audit & profanity](#11e-luồng-audit--blocked-words) · [11.F Notification templates](#11f-luồng-notification-templates) · [11.G Dashboard](#11g-luồng-dashboard-hệ-thống) · [11.H Org CRUD](#11h-luồng-tổ-chức-sở--phường-admin-only)

### Phụ lục

12. [Real-time (SignalR)](#12-real-time-signalr)
13. [Master index theo controller](#13-master-index-theo-controller)

---

## Bản đồ luồng LEO (2 nhánh song song)

Một **Report** có thể chạy **Cleanup** và **Inspection** độc lập (BR-ORG-013):

```
Citizen submit
    → LEO verify (§8.A)
         ├─ Cleanup track (§8.B): assign team / dispatch company → theo dõi progress-board
         └─ Inspection track (§8.C): POST inspections → assign Inspection Team → Inspector Mobile xử lý
              ↑ có thể kích hoạt từ §8.E (nghi tái phát)
```

| Luồng LEO | Mục | FE guide |
|-----------|-----|----------|
| Xác minh & triage | [§8.A](#8a-luồng-xác-minh--triage) | — |
| Dọn dẹp | [§8.B](#8b-luồng-dọn-dẹp-cleanup-track) | — |
| Xử phạt (Inspection) | [§8.C](#8c-luồng-xử-phạt-inspection-track) | `fe-leo-inspection-workflow-guide.md` |
| Trùng lặp | [§8.D](#8d-luồng-phát-hiện-trùng) | `fe-leo-duplicate-detection-guide.md` |
| Nghi tái phát | [§8.E](#8e-luồng-nghi-tái-phạm) | `fe-leo-violation-recurrence-guide.md` |

---

## 1. Public & Catalog

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| PUB-01 | ☐ | ☐ | GET | `/v1/catalog/pollution-categories` | Danh mục loại ô nhiễm |
| PUB-02 | ☐ | ☐ | GET | `/v1/catalog/provinces` | Danh sách tỉnh/TP |
| PUB-03 | ☐ | ☐ | GET | `/v1/catalog/provinces/{provinceCode}/wards` | Phường/xã theo tỉnh |
| PUB-04 | ☐ | ☐ | GET | `/v1/map/reports` | Bản đồ công khai (bbox, mode detail/aggregate) |
| PUB-05 | ☐ | ☐ | GET | `/v1/map/summary` | Thẻ tổng quan khu vực đang xem |

> Guest / chưa đăng nhập: Map + Catalog. Citizen app thường gọi cả hai trước login.

---

## 2. Auth & Session (mọi actor)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Actor chính | Mô tả |
|----|-------|----------|--------|----------|-------------|-------|
| AUTH-01 | ☐ | ☐ | POST | `/v1/auth/register` | Citizen | Đăng ký tài khoản |
| AUTH-02 | ☐ | ☐ | POST | `/v1/auth/login` | Tất cả | Email + password |
| AUTH-03 | ☐ | ☐ | POST | `/v1/auth/google-login` | Citizen (thường) | Firebase Google token |
| AUTH-04 | ☐ | ☐ | POST | `/v1/auth/request-otp` | Tất cả | Gửi OTP email |
| AUTH-05 | ☐ | ☐ | POST | `/v1/auth/verify-otp` | Tất cả | Xác minh OTP |
| AUTH-06 | ☐ | ☐ | POST | `/v1/auth/forgot-password` | Tất cả | Quên mật khẩu |
| AUTH-07 | ☐ | ☐ | POST | `/v1/auth/reset-password` | Tất cả | Đặt lại mật khẩu bằng OTP |
| AUTH-08 | ☐ | ☐ | POST | `/v1/auth/refresh-token` | Tất cả | Refresh token rotation |
| AUTH-09 | ☐ | ☐ | POST | `/v1/auth/change-password` | Tất cả | Đổi mật khẩu (đã login) |

---

## 3. Profile, Media, Notifications (mọi actor)

### 3.1 User profile

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| USR-01 | ☐ | ☐ | GET | `/v1/users/profile` | Hồ sơ của tôi |
| USR-02 | ☐ | ☐ | PUT | `/v1/users/profile` | Cập nhật tên |
| USR-03 | ☐ | ☐ | POST | `/v1/users/avatar` | Upload avatar (multipart) |
| USR-04 | ☐ | ☐ | POST | `/v1/users/phone/verify-firebase` | Xác minh SĐT Firebase |
| USR-05 | ☐ | ☐ | POST | `/v1/users/me/consent` | Chấp nhận xử lý dữ liệu (BR-DAT-005) |
| USR-06 | ☐ | ☐ | GET | `/v1/users/me/data-export` | Export dữ liệu cá nhân |

### 3.2 Media upload (presign → R2)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| MED-01 | ☐ | ☐ | POST | `/v1/media/presign` | Presigned URL (ReportImage, Before, Progress, After, …) |
| MED-02 | ☐ | ☐ | POST | `/v1/media/reports/images` | Upload ảnh báo cáo qua BE (legacy/alternate) |
| MED-03 | ☐ | ☐ | POST | `/v1/media/reports/videos` | Upload video báo cáo |
| MED-04 | ☐ | ☐ | POST | `/v1/media/comments/images` | Upload ảnh comment |

### 3.3 Notifications

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| NTF-01 | ☐ | ☐ | GET | `/v1/notifications` | Danh sách thông báo |
| NTF-02 | ☐ | ☐ | PUT | `/v1/notifications/{id}/read` | Đánh dấu đã đọc |
| NTF-03 | ☐ | ☐ | PUT | `/v1/notifications/read-all` | Đọc tất cả |
| NTF-04 | ☐ | ☐ | GET | `/v1/notifications/preferences` | Cài đặt thông báo |
| NTF-05 | ☐ | ☐ | PUT | `/v1/notifications/preferences` | Cập nhật preferences |
| NTF-06 | ☐ | ☐ | PUT | `/v1/notifications/device-token` | Đăng ký FCM token (Mobile) |

---

## 4. Citizen — Mobile

> **+** Luồng chung: [§1 Public](#1-public--catalog) · [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Media/Notifications](#3-profile-media-notifications-mọi-actor)

### 4.A Luồng gửi báo cáo

```
Catalog/Map → presign → analyze (optional) → POST report (hoặc draft)
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CIT-01 | — | ☐ | POST | `/v1/reports/analyze` | AI phân tích ảnh (multipart) |
| 1 | CIT-02 | — | ☐ | POST | `/v1/reports/analyze-uploaded` | AI phân tích ảnh đã PUT R2 |
| 2 | CIT-03 | — | ☐ | POST | `/v1/reports` | Gửi báo cáo |
| 2 | CIT-04 | — | ☐ | POST | `/v1/reports/drafts` | Lưu nháp |
| 3 | CIT-05 | — | ☐ | GET | `/v1/reports/drafts` | Danh sách nháp |
| 3 | CIT-06 | — | ☐ | DELETE | `/v1/reports/drafts/{draftId}` | Xóa nháp |

> Bước 1 dùng [§3.2 MED-01 presign](#32-media-upload-presign--r2).

### 4.B Luồng theo dõi báo cáo

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CIT-07 | — | ☐ | GET | `/v1/reports/my` | Báo cáo của tôi |
| 2 | CIT-08 | — | ☐ | GET | `/v1/reports/{id}` | Chi tiết báo cáo |
| 2 | CIT-09 | — | ☐ | GET | `/v1/reports/{id}/history` | Timeline status |
| 3 | CIT-10 | — | ☐ | DELETE | `/v1/reports/{id}` | Xóa (Submitted only) |
| 3 | CIT-14 | — | ☐ | POST | `/v1/reports/{id}/flag` | Gắn cờ spam/duplicate |

### 4.C Luồng sau xử lý (Resolved)

```
Resolved → close (hài lòng) | reopen-request | rate (analytics)
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CIT-11 | — | ☐ | PUT | `/v1/reports/{id}/close` | Xác nhận hài lòng → Closed |
| 1 | CIT-12 | — | ☐ | POST | `/v1/reports/{id}/reopen-requests` | Yêu cầu mở lại (+ ảnh) |
| 1 | CIT-13 | — | ☐ | POST | `/v1/reports/{id}/rate` | Đánh giá chất lượng (BR-REP-018) |
| — | CIT-15 | — | ☐ | ⚠️ PUT | `/v1/reports/{id}/reopen` | **Deprecated** |

### 4.D Luồng bình luận & gamification

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CIT-16 | — | ☐ | GET | `/v1/reports/{reportId}/comments` | Xem comment |
| 2 | CIT-17 | — | ☐ | POST | `/v1/reports/{reportId}/comments` | Thêm comment |
| 3 | CIT-18 | — | ☐ | POST | `/v1/comments/{commentId}/like` | Like |
| 3 | CIT-19 | — | ☐ | PUT | `/v1/comments/{commentId}` | Sửa |
| 3 | CIT-20 | — | ☐ | DELETE | `/v1/comments/{commentId}` | Xóa |
| — | CIT-21 | — | ☐ | GET | `/v1/gamification/my-points` | Điểm |
| — | CIT-22 | — | ☐ | GET | `/v1/gamification/my-badges` | Huy hiệu |
| — | CIT-23 | — | ☐ | GET | `/v1/gamification/badges` | Catalog badge |
| — | CIT-24 | — | ☐ | PUT | `/v1/gamification/featured-badge` | Badge nổi bật |
| — | CIT-25 | — | ☐ | GET | `/v1/gamification/leaderboard` | Bảng xếp hạng |

### 4.E Luồng community cleanup (volunteer)

```
Browse events → join → check-in GPS
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CIT-26 | — | ☐ | GET | `/v1/community-cleanups` | Danh sách sự kiện |
| 2 | CIT-27 | — | ☐ | GET | `/v1/community-cleanups/{eventId}` | Chi tiết |
| 2 | CIT-28 | — | ☐ | GET | `/v1/reports/{reportId}/community-cleanup` | Sự kiện gắn báo cáo |
| 3 | CIT-29 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/join` | Tham gia |
| 3 | CIT-30 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/withdraw` | Rút lui |
| — | CIT-31 | — | ☐ | GET | `/v1/community-cleanups/my` | Sự kiện tôi tham gia |
| 4 | CIT-32 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/check-in` | Check-in GPS |

### 4.F Luồng lời mời đội cộng đồng

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CIT-33 | — | ☐ | GET | `/v1/invitations/my` | Lời mời của tôi |
| 2 | CIT-34 | — | ☐ | POST | `/v1/invitations/{invitationId}/accept` | Chấp nhận → đổi role |
| 2 | CIT-35 | — | ☐ | POST | `/v1/invitations/{invitationId}/decline` | Từ chối |

---

## 5. Cleaner — Mobile

> Cleaner = đội dọn cộng đồng (LocalOffice). Role đổi sau [§4.F accept invitation](#4f-luồng-lời-mời-đội-cộng-đồng).  
> **+** [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Media/Notifications](#3-profile-media-notifications-mọi-actor)

### 5.A Luồng thực hiện task dọn dẹp

```
my-tasks → accept → check-in → before-images → progress → resolve
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CLN-01 | — | ☐ | GET | `/v1/teams/my-profile` | Profile team |
| 1 | CLN-02 | — | ☐ | GET | `/v1/teams/my-tasks` | Danh sách task |
| 1 | CLN-03 | — | ☐ | GET | `/v1/teams/my-tasks/progress-stats` | Thống kê tiến độ |
| 2 | CLN-04 | — | ☐ | GET | `/v1/teams/my-tasks/{reportId}` | Chi tiết task |
| 3 | CLN-05 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/accept` | Nhận task |
| 3 | CLN-06 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/decline` | Từ chối |
| 4 | CLN-07 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/check-in` | Check-in GPS (≤200m) |
| 5 | CLN-11 | — | ☐ | GET | `/v1/reports/{id}` | Chi tiết báo cáo |
| 5 | CLN-12 | — | ☐ | GET | `/v1/reports/{id}/progress` | Board tiến độ |
| 6 | CLN-13 | — | ☐ | POST | `/v1/reports/{id}/before-images` | Ảnh hiện trạng |
| 7 | CLN-08 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/progress` | % trên assignment |
| 7 | CLN-14 | — | ☐ | PUT | `/v1/reports/{id}/progress` | Tiến độ + ảnh progress |
| 8 | CLN-15 | — | ☐ | PUT | `/v1/reports/{id}/resolve` | Hoàn thành (≥2 ảnh after) |
| — | CLN-09 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/escalate` | Escalate LEO |
| — | CLN-10 | — | ☐ | GET | `/v1/teams/my-progress` | Lịch sử cá nhân |

### 5.B Luồng dẫn dắt community cleanup

```
led-by-me → start → before → progress → submit-verification → LEO duyệt (§8.G)
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CLN-16 | — | ☐ | GET | `/v1/community-cleanups/led-by-me` | Sự kiện tôi dẫn dắt |
| 2 | CLN-17 | — | ☐ | GET | `/v1/community-cleanups/{eventId}/participants` | Participants |
| 3 | CLN-18 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/start` | Bắt đầu |
| 4 | CLN-19 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/before-images` | Ảnh before |
| 5 | CLN-20 | — | ☐ | PUT | `/v1/community-cleanups/{eventId}/progress` | Tiến độ |
| 6 | CLN-21 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/submit-verification` | Gửi LEO duyệt |

---

## 6. Inspector — Mobile

> **+** [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Media/Notifications](#3-profile-media-notifications-mọi-actor)  
> Guide: `docs/fe-inspection-api-guide.md` · `docs/Changelogs/fe-inspection-checklist-guide.md`

### 6.A Luồng inspection hiện trường

```
queue → accept → confirm-arrival → checklist + evidence → details → submit-field-report
    → issue-penalty HOẶC close-no-violation → record-payment → close
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | INS-01 | — | ☐ | GET | `/v1/inspections/queue` | Hàng đợi inspection |
| 2 | INS-02 | ☐ | ☐ | GET | `/v1/inspections/{id}` | Chi tiết |
| 3 | INS-03 | — | ☐ | POST | `/v1/inspections/{id}/accept` | Nhận nhiệm vụ |
| 3 | INS-04 | — | ☐ | POST | `/v1/inspections/{id}/decline` | Từ chối |
| 4 | INS-05 | — | ☐ | POST | `/v1/inspections/{id}/confirm-arrival` | Xác nhận đến hiện trường |
| 5 | INS-06 | — | ☐ | PUT | `/v1/inspections/{id}/checklist` | Checklist BR-INS-033 |
| 5 | INS-08 | — | ☐ | POST | `/v1/inspections/{id}/evidence` | Metadata evidence |
| 5 | INS-09 | — | ☐ | POST | `/v1/inspections/{id}/evidence-images` | URL ảnh evidence |
| 6 | INS-10 | — | ☐ | PUT | `/v1/inspections/{id}/details` | Biên bản / violator info |
| 7 | INS-07 | — | ☐ | PUT | `/v1/inspections/{id}/submit-field-report` | Khóa checklist |
| 8a | INS-11 | — | ☐ | PUT | `/v1/inspections/{id}/issue-penalty` | Ban hành QĐ xử phạt |
| 8b | INS-12 | — | ☐ | PUT | `/v1/inspections/{id}/close-no-violation` | Không vi phạm |
| 9 | INS-13 | — | ☐ | PUT | `/v1/inspections/{id}/record-payment` | Ghi nhận nộp phạt |
| 9 | INS-14 | — | ☐ | GET | `/v1/inspections/{id}/payments` | Lịch sử thanh toán |
| 9 | INS-15 | — | ☐ | DELETE | `/v1/inspections/payments/{paymentId}` | Xóa payment (sai) |
| 10 | INS-16 | — | ☐ | PUT | `/v1/inspections/{id}/close` | Đóng hồ sơ |
| — | INS-17 | — | ☐ | GET | `/v1/inspections/kpi` | KPI team |
| — | INS-18 | — | ☐ | ⚠️ POST | `/v1/inspections/{id}/check-in` | **Deprecated 410** |
| — | INS-19 | — | ☐ | ⚠️ PUT | `/v1/inspections/{id}/progress` | **Deprecated 410** |

### 6.B Luồng quản lý violating entity

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | INS-20 | — | ☐ | GET | `/v1/violating-entities` | Tra cứu |
| 2 | INS-21 | — | ☐ | GET | `/v1/violating-entities/{id}` | Chi tiết |
| 3 | INS-22 | — | ☐ | POST | `/v1/violating-entities` | Tạo mới |
| 4 | INS-23 | — | ☐ | PATCH | `/v1/violating-entities/{id}` | Cập nhật |
| — | INS-24 | — | ☐ | DELETE | `/v1/violating-entities/{id}` | Xóa |

### 6.C Luồng nhận/từ chối task team

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | INS-25 | — | ☐ | GET | `/v1/teams/my-profile` | Profile team |
| 1 | INS-26 | — | ☐ | GET | `/v1/teams/my-tasks` | Task list |
| 2 | INS-27 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/accept` | Nhận |
| 2 | INS-28 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/decline` | Từ chối |
| — | INS-29 | — | ☐ | GET | `/v1/teams/my-progress` | Lịch sử |

---

## 7. CompanyStaff — Mobile

> Luồng giống [§5.A Cleaner](#5a-luồng-thực-hiện-task-dọn-dẹp) nhưng task do **CompanyManager** phân công.  
> **+** [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Media/Notifications](#3-profile-media-notifications-mọi-actor)

### 7.A Luồng thực hiện task công ty

```
my-tasks → accept → check-in → before-images → progress → resolve
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CST-01 | — | ☐ | GET | `/v1/teams/my-profile` | Profile team công ty |
| 1 | CST-02 | — | ☐ | GET | `/v1/teams/my-tasks` | Task được giao |
| 2 | CST-03 | — | ☐ | GET | `/v1/teams/my-tasks/{reportId}` | Chi tiết task |
| 3 | CST-04 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/accept` | Nhận |
| 3 | CST-05 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/decline` | Từ chối |
| 4 | CST-06 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/check-in` | Check-in GPS |
| 5 | CST-10 | — | ☐ | GET | `/v1/reports/{id}` | Chi tiết báo cáo |
| 5 | CST-11 | — | ☐ | GET | `/v1/reports/{id}/progress` | Tiến độ các team |
| 6 | CST-12 | — | ☐ | POST | `/v1/reports/{id}/before-images` | Ảnh before |
| 7 | CST-07 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/progress` | % assignment |
| 7 | CST-13 | — | ☐ | PUT | `/v1/reports/{id}/progress` | Ảnh + progress |
| 8 | CST-14 | — | ☐ | PUT | `/v1/reports/{id}/resolve` | Hoàn thành |
| — | CST-08 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/escalate` | Escalate |
| — | CST-09 | — | ☐ | GET | `/v1/teams/my-progress` | Lịch sử |

---

## 8. LEO — Web

> **+** [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Notifications](#3-profile-media-notifications-mọi-actor)  
> Xem [Bản đồ luồng LEO](#bản-đồ-luồng-leo-2-nhánh-song-song) — **Cleanup** và **Inspection** chạy song song trên cùng Report.

### 8.A Luồng xác minh & triage

```
queue → detail → verify | reject | escalate
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-01 | ☐ | — | GET | `/v1/reports/queue` | Hàng đợi phường |
| 2 | LEO-02 | ☐ | — | GET | `/v1/reports/{id}` | Chi tiết báo cáo |
| 3 | LEO-03 | ☐ | — | PUT | `/v1/reports/{id}/verify` | Xác minh → Verified |
| 3 | LEO-04 | ☐ | — | PUT | `/v1/reports/{id}/reject` | Từ chối |
| 3 | LEO-07 | ☐ | — | POST | `/v1/reports/{id}/escalate` | Escalate DEO |
| 4 | LEO-09 | ☐ | — | PUT | `/v1/reports/{id}/waste-tags` | Gắn waste tags |
| 4 | LEO-10 | ☐ | — | GET | `/v1/waste-tags` | Danh sách tags |

> Sau verify: rẽ nhánh [§8.B Cleanup](#8b-luồng-dọn-dẹp-cleanup-track) và/hoặc [§8.C Inspection](#8c-luồng-xử-phạt-inspection-track).

### 8.B Luồng dọn dẹp (Cleanup track)

```
assign team / dispatch company → reassign → progress-board (giám sát)
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-05 | ☐ | — | POST | `/v1/reports/{id}/assign` | Phân công team cộng đồng |
| 1 | LEO-08 | ☐ | — | POST | `/v1/reports/{id}/dispatch-to-company` | Điều phối công ty |
| 2 | LEO-06 | ☐ | — | PUT | `/v1/reports/{id}/reassign` | Chuyển giao team |
| 3 | LEO-47 | ☐ | — | GET | `/v1/companies/my-ward` | Dropdown công ty phường |
| 4 | LEO-11 | ☐ | — | GET | `/v1/reports/progress-board` | Bảng tiến độ phường |
| 4 | LEO-12 | ☐ | — | GET | `/v1/reports/{id}/progress` | Chi tiết tiến độ report |

### 8.C Luồng xử phạt (Inspection track)

```
POST inspections (Draft) → assign Inspection Team → giám sát detail/payments
    (Inspector Mobile thực thi §6.A)
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-22 | ☐ | — | POST | `/v1/reports/{id}/inspections` | Mở hồ sơ (body `{}` OK) |
| 2 | LEO-25 | ☐ | — | PUT | `/v1/inspections/{id}/assign-team` | Gán/re-gán Inspection Team |
| 2 | LEO-27 | ☐ | — | GET | `/v1/teams?teamType=Inspection` | Dropdown team |
| 3 | LEO-23 | ☐ | — | GET | `/v1/reports/{id}/inspections` | DS hồ sơ trên report |
| 4 | LEO-24 | ☐ | — | GET | `/v1/inspections/{id}` | Chi tiết (SLA, checklist, QĐ) |
| 5 | LEO-26 | ☐ | — | GET | `/v1/inspections/{id}/payments` | Lịch sử nộp phạt |
| — | LEO-52 | ☐ | — | GET | `/v1/inspections/kpi` | KPI đội inspection |
| — | LEO-51 | ☐ | — | GET | `/v1/violating-entities` | Tra cứu violator |

> Guide: `docs/Changelogs/fe-leo-inspection-workflow-guide.md`

### 8.D Luồng phát hiện trùng

```
duplicate-candidates → confirm | dismiss
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-13 | ☐ | — | GET | `/v1/reports/duplicate-candidates` | DS nghi trùng (+ `media[]`) |
| 2 | LEO-14 | ☐ | — | POST | `/v1/reports/{id}/confirm-duplicate` | Xác nhận trùng |
| 2 | LEO-15 | ☐ | — | POST | `/v1/reports/{id}/dismiss-duplicate` | Bác cờ |

> Guide: `docs/Changelogs/fe-leo-duplicate-detection-guide.md`

### 8.E Luồng nghi tái phát

```
candidates → comparison → dismiss HOẶC → §8.C POST inspections
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-01 | ☐ | — | GET | `/v1/reports/queue?isSuspectedViolationRecurrence=true` | Lọc queue |
| 1 | LEO-16 | ☐ | — | GET | `/v1/reports/violation-recurrence-candidates` | DS candidate |
| 2 | LEO-17 | ☐ | — | GET | `/v1/reports/{id}/violation-recurrence-comparison` | So sánh 2 cột |
| 3 | LEO-18 | ☐ | — | POST | `/v1/reports/{id}/dismiss-violation-recurrence` | Bác cờ |
| 4 | LEO-22 | ☐ | — | POST | `/v1/reports/{id}/inspections` | Mở hồ sơ xử phạt |

> Guide: `docs/Changelogs/fe-leo-violation-recurrence-guide.md`

### 8.F Luồng citizen reopen

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-19 | ☐ | — | GET | `/v1/reports/reopen-requests` | Queue reopen |
| 2 | LEO-20 | ☐ | — | POST | `/v1/reports/{id}/reopen-requests/{requestId}/approve` | Duyệt |
| 2 | LEO-21 | ☐ | — | POST | `/v1/reports/{id}/reopen-requests/{requestId}/reject` | Từ chối |

### 8.G Luồng duyệt community cleanup

```
LEO tạo event → citizen/cleaner thực hiện → office-queue → verify | reject
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-39 | ☐ | — | POST | `/v1/reports/{reportId}/community-cleanups` | Tạo sự kiện |
| 2 | LEO-40 | ☐ | — | GET | `/v1/community-cleanups/office-queue` | Queue duyệt |
| 3 | LEO-41 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/verify` | Duyệt hoàn thành |
| 3 | LEO-42 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/reject-verification` | Từ chối |
| — | LEO-43 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/close-join` | Đóng đăng ký |
| — | LEO-44 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/cancel` | Hủy sự kiện |

### 8.H Luồng quản lý team & staff

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-27 | ☐ | — | GET | `/v1/teams` | DS team phường |
| 2 | LEO-28 | ☐ | — | GET | `/v1/teams/{id}` | Chi tiết team |
| 3 | LEO-29 | ☐ | — | POST | `/v1/teams` | Tạo team |
| 3 | LEO-30 | ☐ | — | PUT | `/v1/teams/{id}` | Sửa team |
| 4 | LEO-31 | ☐ | — | POST | `/v1/teams/{teamId}/members` | Thêm thành viên |
| 4 | LEO-32 | ☐ | — | DELETE | `/v1/teams/{teamId}/members/{userId}` | Xóa thành viên |
| 4 | LEO-33 | ☐ | — | PUT | `/v1/teams/{teamId}/members/{userId}/transfer` | Chuyển team |
| — | LEO-34 | ☐ | — | GET | `/v1/offices/my/reports` | Báo cáo office |
| — | LEO-35 | ☐ | — | GET | `/v1/offices/my/staff` | Staff phường |
| — | LEO-36 | ☐ | — | GET | `/v1/offices/my/staff/lookup` | Tra user mời |
| — | LEO-37 | ☐ | — | POST | `/v1/offices/my/staff` | Mời staff |
| — | LEO-38 | ☐ | — | DELETE | `/v1/offices/my/staff/{userId}` | Gỡ staff |
| — | LEO-48 | ☐ | — | GET | `/v1/offices` | DS office (read) |
| — | LEO-49 | ☐ | — | GET | `/v1/offices/{id}` | Chi tiết office |

### 8.I Luồng KPI & báo cáo

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | LEO-45 | ☐ | — | GET | `/v1/reports/officer-kpi` | KPI cá nhân LEO |
| 2 | LEO-46 | ☐ | — | GET | `/v1/reports/export` | Export CSV phường |
| — | LEO-50 | ☐ | — | POST | `/v1/comments/comments/{commentId}/hide` | Ẩn comment |

---

## 9. DEO — Web

> **+** [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Notifications](#3-profile-media-notifications-mọi-actor)

### 9.A Luồng quản trị sở & phường

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | DEO-01 | ☐ | — | GET | `/v1/departments` | Danh sách sở |
| 2 | DEO-02 | ☐ | — | GET | `/v1/departments/{id}` | Chi tiết sở |
| 3 | DEO-03 | ☐ | — | GET | `/v1/departments/my-offices` | Office thuộc sở |
| 4 | DEO-05 | ☐ | — | GET | `/v1/offices` | Danh sách phường |
| 5 | DEO-06 | ☐ | — | GET | `/v1/offices/{id}` | Chi tiết phường |

### 9.B Luồng quản lý công ty đối tác

```
companies CRUD → service-areas → contract → KPI
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | DEO-07 | ☐ | — | POST | `/v1/companies` | Tạo công ty |
| 1 | DEO-08 | ☐ | — | GET | `/v1/companies` | Danh sách |
| 2 | DEO-09 | ☐ | — | GET | `/v1/companies/{id}` | Chi tiết |
| 3 | DEO-10 | ☐ | — | POST | `/v1/companies/{id}/manager` | Gán CompanyManager |
| 3 | DEO-11 | ☐ | — | POST | `/v1/companies/{id}/manager/{userId}/reset-password` | Reset MK |
| 4 | DEO-12 | ☐ | — | POST | `/v1/companies/{id}/suspend` | Tạm ngưng |
| 4 | DEO-13 | ☐ | — | POST | `/v1/companies/{id}/terminate` | Chấm dứt HĐ |
| 4 | DEO-14 | ☐ | — | POST | `/v1/companies/{id}/reactivate` | Kích hoạt lại |
| — | DEO-15 | ☐ | — | DELETE | `/v1/companies/{id}` | Xóa |
| 5 | DEO-16 | ☐ | — | GET | `/v1/companies/{id}/service-areas` | Vùng phục vụ |
| 5 | DEO-17 | ☐ | — | PUT | `/v1/companies/{id}/service-areas` | Cập nhật vùng |
| 6 | DEO-18 | ☐ | — | POST | `/v1/companies/{id}/renew-contract` | Gia hạn HĐ |
| 6 | DEO-19 | ☐ | — | GET | `/v1/companies/{id}/contract-history` | Lịch sử HĐ |
| — | DEO-20 | ☐ | — | GET | `/v1/companies/{id}/kpi` | KPI công ty |

### 9.C Luồng giám sát báo cáo & team (scope tỉnh)

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | DEO-04 | ☐ | — | GET | `/v1/departments/my/reports` | Báo cáo cấp tỉnh |
| 1 | DEO-21 | ☐ | — | GET | `/v1/reports/queue` | Hàng đợi escalated |
| 2 | DEO-22 | ☐ | — | GET | `/v1/reports/{id}` | Chi tiết |
| 3 | DEO-23 | ☐ | — | GET | `/v1/reports/export` | Export toàn tỉnh |
| — | DEO-24 | ☐ | — | GET | `/v1/reports/officer-kpi` | KPI (officerId param) |
| — | DEO-25 | ☐ | — | GET | `/v1/teams` | Teams scope |
| — | DEO-26 | ☐ | — | GET | `/v1/teams/{id}` | Chi tiết team |
| — | DEO-27 | ☐ | — | POST | `/v1/comments/comments/{commentId}/hide` | Moderation |

> DEO read-only inspection: dùng chung `GET /v1/inspections/{id}` (role LEO/Inspector/Admin).

---

## 10. CompanyManager — Web

> **+** [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Notifications](#3-profile-media-notifications-mọi-actor)

### 10.A Luồng quản lý công ty & nhân sự

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CMG-01 | ☐ | — | GET | `/v1/companies/my` | Thông tin công ty |
| 2 | CMG-02 | ☐ | — | GET | `/v1/companies/my/contract-history` | Lịch sử HĐ |
| 3 | CMG-03 | ☐ | — | GET | `/v1/companies/my/kpi` | KPI công ty |
| 4 | CMG-04 | ☐ | — | POST | `/v1/companies/my/staff` | Tạo staff |
| 4 | CMG-05 | ☐ | — | GET | `/v1/companies/my/staff` | DS staff |
| 5 | CMG-06 | ☐ | — | PUT | `/v1/companies/my/staff/{userId}/status` | Kích hoạt/vô hiệu |

### 10.B Luồng quản lý team công ty

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CMG-07 | ☐ | — | GET | `/v1/teams/company-teams` | Teams công ty |
| 2 | CMG-08 | ☐ | — | POST | `/v1/teams/company-teams` | Tạo team |
| 3 | CMG-09 | ☐ | — | PUT | `/v1/teams/company-teams/{id}` | Sửa team |
| 3 | CMG-10 | ☐ | — | PUT | `/v1/teams/company-teams/{id}/archive` | Lưu trữ |
| 3 | CMG-11 | ☐ | — | DELETE | `/v1/teams/company-teams/{id}` | Xóa team |
| 4 | CMG-12 | ☐ | — | POST | `/v1/teams/company-teams/{teamId}/members` | Thêm nhân viên |
| 4 | CMG-13 | ☐ | — | DELETE | `/v1/teams/company-teams/{teamId}/members/{userId}` | Gỡ nhân viên |
| — | CMG-14 | ☐ | — | GET | `/v1/teams/{id}` | Chi tiết team |

### 10.C Luồng phân công & theo dõi task (Cleanup)

```
company-queue → assign-company-team → company-assignments → progress
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CMG-15 | ☐ | — | GET | `/v1/reports/company-queue` | Task chờ phân công |
| 2 | CMG-16 | ☐ | — | POST | `/v1/reports/{id}/assign-company-team` | Phân công team |
| 3 | CMG-17 | ☐ | — | GET | `/v1/reports/company-assignments` | Task đã giao |
| 4 | CMG-18 | ☐ | — | GET | `/v1/reports/company-assignments/{reportId}` | Chi tiết tiến độ |
| 4 | CMG-19 | ☐ | — | GET | `/v1/reports/{id}/progress` | Board tiến độ |

### 10.D Luồng dashboard analytics

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | CMG-20 | ☐ | — | GET | `/v1/dashboard/company/overview` | Tổng quan |
| — | CMG-21 | ☐ | — | GET | `/v1/dashboard/company/workload-trend` | Xu hướng workload |
| — | CMG-22 | ☐ | — | GET | `/v1/dashboard/company/task-status` | Phân bố trạng thái |
| — | CMG-23 | ☐ | — | GET | `/v1/dashboard/company/team-performance` | Hiệu suất team |
| — | CMG-24 | ☐ | — | GET | `/v1/dashboard/company/staff-performance` | Hiệu suất staff |
| — | CMG-25 | ☐ | — | GET | `/v1/dashboard/company/queue-aging` | Tuổi hàng đợi |
| — | CMG-26 | ☐ | — | GET | `/v1/dashboard/company/recent-activities` | Hoạt động gần đây |
| — | CMG-27 | ☐ | — | GET | `/v1/dashboard/company/upcoming-deadlines` | Deadline sắp tới |

---

## 11. Admin — Web

> **+** [§2 Auth](#2-auth--session-mọi-actor) · [§3 Profile/Notifications](#3-profile-media-notifications-mọi-actor)  
> Admin có thể gọi thêm hầu hết endpoint LEO/DEO/CompanyManager (role `Admin`).

### 11.A Luồng quản lý user & role

```
users list → detail → edit / role / ban
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | ADM-03 | ☐ | — | GET | `/v1/admin/users` | Users (paginated) |
| 1 | ADM-02 | ☐ | — | GET | `/v1/admin/users/all` | All users (no paging) |
| 2 | ADM-04 | ☐ | — | GET | `/v1/admin/users/{id}` | Chi tiết user |
| 3 | ADM-01 | ☐ | — | POST | `/v1/admin/users` | Tạo user |
| 3 | ADM-05 | ☐ | — | PUT | `/v1/admin/users/{id}` | Sửa user |
| 3 | ADM-07 | ☐ | — | PUT | `/v1/admin/users/{id}/role` | Đổi role |
| 3 | ADM-08 | ☐ | — | PUT | `/v1/admin/users/{id}/ban` | Ban/unban |
| 3 | ADM-06 | ☐ | — | DELETE | `/v1/admin/users/{id}` | Soft delete |

### 11.B Luồng kiểm duyệt báo cáo

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | ADM-09 | ☐ | — | GET | `/v1/admin/reports` | All reports |
| 1 | ADM-14 | ☐ | — | GET | `/v1/admin/spam-suspects` | Nghi spam |
| 2 | ADM-10 | ☐ | — | GET | `/v1/admin/reports/{id}` | Chi tiết |
| 3 | ADM-11 | ☐ | — | PUT | `/v1/admin/reports/{id}/status` | Override status |
| 3 | ADM-12 | ☐ | — | POST | `/v1/admin/reports/{id}/hide` | Ẩn báo cáo |
| 3 | ADM-13 | ☐ | — | POST | `/v1/admin/reports/{id}/unhide` | Bỏ ẩn |

### 11.C Luồng cấu hình danh mục

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| — | ADM-15 | ☐ | — | GET | `/v1/admin/pollution-categories` | CRUD categories |
| — | ADM-16 | ☐ | — | POST | `/v1/admin/pollution-categories` | Tạo |
| — | ADM-17 | ☐ | — | PUT | `/v1/admin/pollution-categories/{id}` | Sửa |
| — | ADM-18 | ☐ | — | DELETE | `/v1/admin/pollution-categories/{id}` | Xóa |
| — | ADM-19 | ☐ | — | PUT | `/v1/admin/pollution-categories/{id}/archive` | Archive |
| — | ADM-20 | ☐ | — | GET | `/v1/admin/waste-tags` | Waste tags |
| — | ADM-21 | ☐ | — | POST | `/v1/admin/waste-tags` | Tạo |
| — | ADM-22 | ☐ | — | PUT | `/v1/admin/waste-tags/{id}` | Sửa |
| — | ADM-23 | ☐ | — | PATCH | `/v1/admin/waste-tags/{id}/toggle` | Bật/tắt |
| — | ADM-24 | ☐ | — | DELETE | `/v1/admin/waste-tags/{id}` | Xóa |

### 11.D Luồng RBAC, penalty & gamification

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| — | ADM-25 | ☐ | — | GET | `/v1/admin/roles` | Roles |
| — | ADM-26 | ☐ | — | GET | `/v1/admin/permissions` | Permissions |
| — | ADM-27 | ☐ | — | GET | `/v1/admin/penalty-frameworks` | Khung xử phạt |
| — | ADM-28 | ☐ | — | POST | `/v1/admin/penalty-frameworks` | Tạo |
| — | ADM-29 | ☐ | — | PUT | `/v1/admin/penalty-frameworks/{id}` | Sửa |
| — | ADM-30 | ☐ | — | PATCH | `/v1/admin/penalty-frameworks/{id}/toggle` | Toggle |
| — | ADM-31 | ☐ | — | GET | `/v1/admin/gamification-configs` | Cấu hình điểm/badge |
| — | ADM-32 | ☐ | — | PUT | `/v1/admin/gamification-configs/{id}` | Cập nhật |
| — | ADM-33 | ☐ | — | POST | `/v1/gamification/{userId}/lock` | Khóa gamification user |

### 11.E Luồng audit & blocked words

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | ADM-34 | ☐ | — | GET | `/v1/admin/audit-logs` | Audit logs |
| 1 | ADM-36 | ☐ | — | GET | `/v1/admin/audit-logs/stats` | Thống kê |
| 2 | ADM-37 | ☐ | — | GET | `/v1/admin/audit-logs/{id}` | Chi tiết log |
| — | ADM-35 | ☐ | — | GET | `/v1/admin/audit-logs/export` | Export audit |
| — | ADM-38 | ☐ | — | GET | `/v1/admin/blocked-words` | Từ cấm |
| — | ADM-39 | ☐ | — | POST | `/v1/admin/blocked-words` | Thêm |
| — | ADM-40 | ☐ | — | PUT | `/v1/admin/blocked-words/{id}` | Sửa |
| — | ADM-41 | ☐ | — | DELETE | `/v1/admin/blocked-words/{id}` | Xóa |

### 11.F Luồng notification templates

```
list → create/edit → publish → test send
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | ADM-42 | ☐ | — | GET | `/v1/admin/notification-templates` | Templates |
| 2 | ADM-43 | ☐ | — | POST | `/v1/admin/notification-templates` | Tạo |
| 2 | ADM-44 | ☐ | — | GET | `/v1/admin/notification-templates/{id}` | Chi tiết |
| 2 | ADM-45 | ☐ | — | PUT | `/v1/admin/notification-templates/{id}` | Sửa |
| 3 | ADM-47 | ☐ | — | PATCH | `/v1/admin/notification-templates/{id}/publish` | Publish |
| 3 | ADM-48 | ☐ | — | POST | `/v1/admin/notification-templates/{id}/test` | Gửi test |
| — | ADM-46 | ☐ | — | DELETE | `/v1/admin/notification-templates/{id}` | Xóa |

### 11.G Luồng dashboard hệ thống

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | ADM-49 | ☐ | — | GET | `/v1/dashboard/admin/overview` | Tổng quan hệ thống |
| — | ADM-50 | ☐ | — | GET | `/v1/dashboard/admin/report-status` | Phân bố status |
| — | ADM-51 | ☐ | — | GET | `/v1/dashboard/admin/report-trend` | Xu hướng báo cáo |
| — | ADM-52 | ☐ | — | GET | `/v1/dashboard/admin/pollution-analytics` | Phân tích loại ô nhiễm |
| — | ADM-53 | ☐ | — | GET | `/v1/dashboard/admin/geographic` | Phân bố địa lý |
| — | ADM-54 | ☐ | — | GET | `/v1/dashboard/admin/report-funnel` | Funnel |
| — | ADM-55 | ☐ | — | GET | `/v1/dashboard/admin/company-performance` | Hiệu suất công ty |
| — | ADM-56 | ☐ | — | GET | `/v1/dashboard/admin/officer-performance` | Hiệu suất officer |
| — | ADM-57 | ☐ | — | GET | `/v1/dashboard/admin/queue-aging` | Tuổi queue |
| — | ADM-58 | ☐ | — | GET | `/v1/dashboard/admin/resolution-distribution` | Phân bố resolution |
| — | ADM-59 | ☐ | — | GET | `/v1/dashboard/admin/recent-activities` | Hoạt động |
| — | ADM-60 | ☐ | — | GET | `/v1/dashboard/admin/alerts` | Cảnh báo hệ thống |

### 11.H Luồng tổ chức sở & phường (Admin-only)

```
departments CRUD → assign DEO → offices CRUD → assign LEO
```

| Bước | ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|------|-----|-------|----------|--------|----------|-------|
| 1 | ADM-61 | ☐ | — | POST | `/v1/departments` | Tạo sở |
| 1 | ADM-62 | ☐ | — | PUT | `/v1/departments/{id}` | Sửa sở |
| 1 | ADM-63 | ☐ | — | DELETE | `/v1/departments/{id}` | Xóa sở |
| 2 | ADM-64 | ☐ | — | PUT | `/v1/departments/{id}/officer` | Gán DEO |
| 3 | ADM-65 | ☐ | — | POST | `/v1/offices` | Tạo phường |
| 3 | ADM-66 | ☐ | — | PUT | `/v1/offices/{id}` | Sửa phường |
| 4 | ADM-67 | ☐ | — | PUT | `/v1/offices/{id}/officer` | Gán LEO |

---

## 12. Real-time (SignalR)

| ID | ☐ Web | ☐ Mobile | Protocol | Path | Mô tả |
|----|-------|----------|----------|------|-------|
| RT-01 | ☐ | ☐ | WebSocket | `/hubs/notifications` | Hub thông báo real-time (JWT) |

> Mobile: kết hợp với `PUT /v1/notifications/device-token` (FCM). Web: có thể chỉ dùng polling hoặc SignalR.

---

## 13. Master index theo controller

Bảng tra cứu nhanh **toàn bộ** route — dùng khi audit cross-team.

| Controller | Prefix | Endpoints |
|------------|--------|-----------|
| AuthController | `/v1/auth` | 9 |
| CatalogController | `/v1/catalog` | 3 |
| MapController | `/v1/map` | 2 |
| UsersController | `/v1/users` | 6 |
| MediaController | `/v1/media` | 4 |
| NotificationsController | `/v1/notifications` | 6 |
| ReportsController | `/v1/reports` + `/v1/waste-tags` | 47 |
| CommentsController | `/v1/reports/.../comments`, `/v1/comments/...` | 6 |
| GamificationController | `/v1/gamification` | 6 |
| TeamsController | `/v1/teams` | 24 |
| InspectionsController | `/v1/inspections` | 20 |
| CommunityCleanupsController | `/v1/community-cleanups` + nested | 19 |
| InvitationsController | `/v1/invitations` | 3 |
| LocalOfficesController | `/v1/offices` | 10 |
| DepartmentsController | `/v1/departments` | 8 |
| CompaniesController | `/v1/companies` | 21 |
| ViolatingEntitiesController | `/v1/violating-entities` | 5 |
| AdminController | `/v1/admin` | 47 |
| AdminDashboardController | `/v1/dashboard/admin` | 12 |
| CompanyDashboardController | `/v1/dashboard/company` | 8 |

**Tổng HTTP endpoints:** ~258 (không tính SignalR).

---

## Ghi chú triển khai FE

| Chủ đề | Tài liệu tham khảo trong repo |
|--------|-------------------------------|
| Duplicate detection (LEO) | `docs/Changelogs/fe-leo-duplicate-detection-guide.md` |
| Violation recurrence (LEO) | `docs/Changelogs/fe-leo-violation-recurrence-guide.md` |
| **Inspection workflow (LEO)** | `docs/Changelogs/fe-leo-inspection-workflow-guide.md` |
| Citizen satisfaction / rate | `docs/Changelogs/fe-citizen-satisfaction-api-guide.md` |
| Comments | `docs/Changelogs/fe-comments-api-guide.md` |
| Inspection checklist | `docs/fe-inspection-api-guide.md` |
| Company Manager | `docs/fe-company-manager-api-guide.md` |
| Company Staff (Mobile) | `docs/fe-company-staff-api-guide.md` |

### Response envelope

Mọi API JSON trả `{ code, message, status, data }` — xem `00_API_CONVENTIONS.md` (nếu có trong repo FE).

### Presign upload flow (Mobile)

```
POST /v1/media/presign → PUT file lên R2 → POST /v1/reports (hoặc before-images / progress / evidence-images)
```

---

## Changelog file này

| Ngày | Thay đổi |
|------|----------|
| 2026-08-03 | Tái cấu trúc theo **luồng nghiệp vụ** (workflow-first): LEO tách Cleanup vs Inspection; thêm cột Bước, sơ đồ luồng, link FE guides |
| 2026-07-30 | Tạo checklist ban đầu từ 20 controllers; bổ sung `violation-recurrence-candidates`, `media[]` trên candidate APIs |

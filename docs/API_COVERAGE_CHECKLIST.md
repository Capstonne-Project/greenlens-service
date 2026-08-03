# GreenLens — API Coverage Checklist (FE Web & Mobile)

> **Mục đích:** Đối chiếu repo **FE Web** và **Mobile** xem đã gọi đủ API backend chưa, tránh missing tính năng theo từng actor.  
> **Nguồn:** Quét trực tiếp `src/Greenlens.Api/Controllers/` · branch `develop` · cập nhật **2026-07-30**.  
> **Base URL API:** `/v1` · **SignalR:** `/hubs/notifications`

---

## Cách dùng

1. Mỗi dòng có cột **☐ Web** và **☐ Mobile** — tick (`x`) khi đã tìm thấy HTTP call tương ứng trong repo FE/Mobile.
2. **`—`** = platform không dùng endpoint này (theo thiết kế actor → device).
3. **`⚠️`** = deprecated / không nên integrate mới.
4. **Admin** có thể gọi hầu hết endpoint có role `Admin` — section Admin liệt kê đầy đủ; các section khác chỉ ghi khi Admin cũng là consumer chính.
5. Tìm kiếm nhanh trong repo FE: `rg "/v1/reports/queue"` hoặc tên path không prefix.

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

1. [Public & Catalog](#1-public--catalog)
2. [Auth & Session (mọi actor)](#2-auth--session-mọi-actor)
3. [Profile, Media, Notifications (mọi actor)](#3-profile-media-notifications-mọi-actor)
4. [Citizen — Mobile](#4-citizen--mobile)
5. [Cleaner — Mobile](#5-cleaner--mobile)
6. [Inspector — Mobile](#6-inspector--mobile)
7. [CompanyStaff — Mobile](#7-companystaff--mobile)
8. [LEO — Web](#8-leo--web)
9. [DEO — Web](#9-deo--web)
10. [CompanyManager — Web](#10-companymanager--web)
11. [Admin — Web](#11-admin--web)
12. [Real-time (SignalR)](#12-real-time-signalr)
13. [Master index theo controller](#13-master-index-theo-controller)

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

### 4.1 Báo cáo — tạo & quản lý

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CIT-01 | — | ☐ | POST | `/v1/reports/analyze` | AI phân tích ảnh (multipart, Step 1) |
| CIT-02 | — | ☐ | POST | `/v1/reports/analyze-uploaded` | AI phân tích ảnh đã PUT R2 |
| CIT-03 | — | ☐ | POST | `/v1/reports` | Gửi báo cáo |
| CIT-04 | — | ☐ | POST | `/v1/reports/drafts` | Lưu nháp |
| CIT-05 | — | ☐ | GET | `/v1/reports/drafts` | Danh sách nháp |
| CIT-06 | — | ☐ | DELETE | `/v1/reports/drafts/{draftId}` | Xóa nháp |
| CIT-07 | — | ☐ | GET | `/v1/reports/my` | Báo cáo của tôi |
| CIT-08 | — | ☐ | GET | `/v1/reports/{id}` | Chi tiết báo cáo |
| CIT-09 | — | ☐ | GET | `/v1/reports/{id}/history` | Timeline status |
| CIT-10 | — | ☐ | DELETE | `/v1/reports/{id}` | Xóa báo cáo (Submitted only) |

### 4.2 Sau xử lý — close / reopen / rate

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CIT-11 | — | ☐ | PUT | `/v1/reports/{id}/close` | Xác nhận hài lòng → Closed |
| CIT-12 | — | ☐ | POST | `/v1/reports/{id}/reopen-requests` | Yêu cầu mở lại (+ ảnh) |
| CIT-13 | — | ☐ | POST | `/v1/reports/{id}/rate` | Đánh giá chất lượng (BR-REP-018) |
| CIT-14 | — | ☐ | POST | `/v1/reports/{id}/flag` | Gắn cờ spam/duplicate/… |
| CIT-15 | — | ☐ | ⚠️ PUT | `/v1/reports/{id}/reopen` | **Deprecated** — dùng reopen-requests |

### 4.3 Comments & gamification

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CIT-16 | — | ☐ | GET | `/v1/reports/{reportId}/comments` | Xem comment |
| CIT-17 | — | ☐ | POST | `/v1/reports/{reportId}/comments` | Thêm comment |
| CIT-18 | — | ☐ | POST | `/v1/comments/{commentId}/like` | Like comment |
| CIT-19 | — | ☐ | PUT | `/v1/comments/{commentId}` | Sửa comment của mình |
| CIT-20 | — | ☐ | DELETE | `/v1/comments/{commentId}` | Xóa comment |
| CIT-21 | — | ☐ | GET | `/v1/gamification/my-points` | Điểm của tôi |
| CIT-22 | — | ☐ | GET | `/v1/gamification/my-badges` | Huy hiệu của tôi |
| CIT-23 | — | ☐ | GET | `/v1/gamification/badges` | Catalog huy hiệu |
| CIT-24 | — | ☐ | PUT | `/v1/gamification/featured-badge` | Chọn badge nổi bật |
| CIT-25 | — | ☐ | GET | `/v1/gamification/leaderboard` | Bảng xếp hạng |

### 4.4 Community cleanup (citizen volunteer)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CIT-26 | — | ☐ | GET | `/v1/community-cleanups` | Danh sách sự kiện |
| CIT-27 | — | ☐ | GET | `/v1/community-cleanups/{eventId}` | Chi tiết sự kiện |
| CIT-28 | — | ☐ | GET | `/v1/reports/{reportId}/community-cleanup` | Sự kiện gắn báo cáo |
| CIT-29 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/join` | Tham gia |
| CIT-30 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/withdraw` | Rút lui |
| CIT-31 | — | ☐ | GET | `/v1/community-cleanups/my` | Sự kiện tôi tham gia |
| CIT-32 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/check-in` | Check-in GPS sự kiện |

### 4.5 Lời mời đội cộng đồng (Citizen → Cleaner/Inspector)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CIT-33 | — | ☐ | GET | `/v1/invitations/my` | Lời mời của tôi |
| CIT-34 | — | ☐ | POST | `/v1/invitations/{invitationId}/accept` | Chấp nhận → đổi role |
| CIT-35 | — | ☐ | POST | `/v1/invitations/{invitationId}/decline` | Từ chối |

> **+** Toàn bộ mục [2. Auth](#2-auth--session-mọi-actor), [3. Profile/Media/Notifications](#3-profile-media-notifications-mọi-actor), [1. Public](#1-public--catalog).

---

## 5. Cleaner — Mobile

> Cleaner = đội dọn cộng đồng (LocalOffice). Sau khi accept invitation, role đổi từ Citizen.

### 5.1 Task assignment

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CLN-01 | — | ☐ | GET | `/v1/teams/my-profile` | Profile team |
| CLN-02 | — | ☐ | GET | `/v1/teams/my-tasks` | Danh sách task |
| CLN-03 | — | ☐ | GET | `/v1/teams/my-tasks/progress-stats` | Thống kê tiến độ |
| CLN-04 | — | ☐ | GET | `/v1/teams/my-tasks/{reportId}` | Chi tiết task |
| CLN-05 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/accept` | Nhận task |
| CLN-06 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/decline` | Từ chối task |
| CLN-07 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/check-in` | Check-in GPS (≤200m) |
| CLN-08 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/progress` | Cập nhật % trên assignment |
| CLN-09 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/escalate` | Escalate lên LEO |
| CLN-10 | — | ☐ | GET | `/v1/teams/my-progress` | Lịch sử tiến độ cá nhân |

### 5.2 Xử lý báo cáo (leader)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CLN-11 | — | ☐ | GET | `/v1/reports/{id}` | Chi tiết báo cáo được giao |
| CLN-12 | — | ☐ | GET | `/v1/reports/{id}/progress` | Board tiến độ các team |
| CLN-13 | — | ☐ | POST | `/v1/reports/{id}/before-images` | Lưu URL ảnh hiện trạng |
| CLN-14 | — | ☐ | PUT | `/v1/reports/{id}/progress` | Cập nhật tiến độ + ảnh progress |
| CLN-15 | — | ☐ | PUT | `/v1/reports/{id}/resolve` | Hoàn thành (≥2 ảnh after) |

### 5.3 Community cleanup (leader)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CLN-16 | — | ☐ | GET | `/v1/community-cleanups/led-by-me` | Sự kiện tôi dẫn dắt |
| CLN-17 | — | ☐ | GET | `/v1/community-cleanups/{eventId}/participants` | Danh sách participant |
| CLN-18 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/start` | Bắt đầu sự kiện |
| CLN-19 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/before-images` | Ảnh before sự kiện |
| CLN-20 | — | ☐ | PUT | `/v1/community-cleanups/{eventId}/progress` | Cập nhật tiến độ |
| CLN-21 | — | ☐ | POST | `/v1/community-cleanups/{eventId}/submit-verification` | Gửi LEO duyệt |

> **+** Auth, Profile, Media presign, Notifications, Gamification (read).

---

## 6. Inspector — Mobile

### 6.1 Inspection queue & workflow (BR-INS-033 checklist)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| INS-01 | — | ☐ | GET | `/v1/inspections/queue` | Hàng đợi inspection |
| INS-02 | ☐ | ☐ | GET | `/v1/inspections/{id}` | Chi tiết (Inspector + LEO xem) |
| INS-03 | — | ☐ | POST | `/v1/inspections/{id}/accept` | Nhận nhiệm vụ |
| INS-04 | — | ☐ | POST | `/v1/inspections/{id}/decline` | Từ chối |
| INS-05 | — | ☐ | POST | `/v1/inspections/{id}/confirm-arrival` | Xác nhận đến hiện trường |
| INS-06 | — | ☐ | PUT | `/v1/inspections/{id}/checklist` | Checklist BR-INS-033 |
| INS-07 | — | ☐ | PUT | `/v1/inspections/{id}/submit-field-report` | Báo cáo hiện trường |
| INS-08 | — | ☐ | POST | `/v1/inspections/{id}/evidence` | Upload evidence metadata |
| INS-09 | — | ☐ | POST | `/v1/inspections/{id}/evidence-images` | Lưu URL ảnh evidence |
| INS-10 | — | ☐ | PUT | `/v1/inspections/{id}/details` | Cập nhật violator info |
| INS-11 | — | ☐ | PUT | `/v1/inspections/{id}/issue-penalty` | Lập biên bản xử phạt |
| INS-12 | — | ☐ | PUT | `/v1/inspections/{id}/close-no-violation` | Đóng — không vi phạm |
| INS-13 | — | ☐ | PUT | `/v1/inspections/{id}/record-payment` | Ghi nhận thanh toán |
| INS-14 | — | ☐ | GET | `/v1/inspections/{id}/payments` | Lịch sử thanh toán |
| INS-15 | — | ☐ | DELETE | `/v1/inspections/payments/{paymentId}` | Xóa payment (nếu sai) |
| INS-16 | — | ☐ | PUT | `/v1/inspections/{id}/close` | Đóng inspection |
| INS-17 | — | ☐ | GET | `/v1/inspections/kpi` | KPI team inspection |
| INS-18 | — | ☐ | ⚠️ POST | `/v1/inspections/{id}/check-in` | **Deprecated 410** |
| INS-19 | — | ☐ | ⚠️ PUT | `/v1/inspections/{id}/progress` | **Deprecated 410** |

### 6.2 Violating entities & tasks

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| INS-20 | — | ☐ | GET | `/v1/violating-entities` | Tra cứu đối tượng vi phạm |
| INS-21 | — | ☐ | GET | `/v1/violating-entities/{id}` | Chi tiết |
| INS-22 | — | ☐ | POST | `/v1/violating-entities` | Tạo mới |
| INS-23 | — | ☐ | PATCH | `/v1/violating-entities/{id}` | Cập nhật |
| INS-24 | — | ☐ | DELETE | `/v1/violating-entities/{id}` | Xóa |

### 6.3 Team tasks (accept/decline only — không check-in cleanup)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| INS-25 | — | ☐ | GET | `/v1/teams/my-profile` | Profile team |
| INS-26 | — | ☐ | GET | `/v1/teams/my-tasks` | Task list |
| INS-27 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/accept` | Nhận |
| INS-28 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/decline` | Từ chối |
| INS-29 | — | ☐ | GET | `/v1/teams/my-progress` | Lịch sử |

> **+** Auth, Profile, Media presign, Notifications.

---

## 7. CompanyStaff — Mobile

> Luồng tương tự **Cleaner** nhưng task do **CompanyManager** phân công qua công ty dịch vụ.

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CST-01 | — | ☐ | GET | `/v1/teams/my-profile` | Profile team công ty |
| CST-02 | — | ☐ | GET | `/v1/teams/my-tasks` | Task được giao |
| CST-03 | — | ☐ | GET | `/v1/teams/my-tasks/{reportId}` | Chi tiết task |
| CST-04 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/accept` | Nhận task |
| CST-05 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/decline` | Từ chối |
| CST-06 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/check-in` | Check-in GPS |
| CST-07 | — | ☐ | PUT | `/v1/teams/my-tasks/{reportId}/progress` | Tiến độ assignment |
| CST-08 | — | ☐ | POST | `/v1/teams/my-tasks/{reportId}/escalate` | Escalate |
| CST-09 | — | ☐ | GET | `/v1/teams/my-progress` | Lịch sử |
| CST-10 | — | ☐ | GET | `/v1/reports/{id}` | Chi tiết báo cáo |
| CST-11 | — | ☐ | GET | `/v1/reports/{id}/progress` | Tiến độ các team |
| CST-12 | — | ☐ | POST | `/v1/reports/{id}/before-images` | Ảnh before |
| CST-13 | — | ☐ | PUT | `/v1/reports/{id}/progress` | Ảnh + % progress |
| CST-14 | — | ☐ | PUT | `/v1/reports/{id}/resolve` | Hoàn thành |

> **+** Auth, Profile, Media presign, Notifications. **Không** dùng community cleanup leader APIs.

---

## 8. LEO — Web

### 8.1 Hàng đợi & xử lý báo cáo

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| LEO-01 | ☐ | — | GET | `/v1/reports/queue` | Hàng đợi LEO (Submitted/Verified/Reopened) |
| LEO-02 | ☐ | — | GET | `/v1/reports/{id}` | Chi tiết báo cáo |
| LEO-03 | ☐ | — | PUT | `/v1/reports/{id}/verify` | Xác minh |
| LEO-04 | ☐ | — | PUT | `/v1/reports/{id}/reject` | Từ chối |
| LEO-05 | ☐ | — | POST | `/v1/reports/{id}/assign` | Phân công team cộng đồng |
| LEO-06 | ☐ | — | PUT | `/v1/reports/{id}/reassign` | Chuyển giao team |
| LEO-07 | ☐ | — | POST | `/v1/reports/{id}/escalate` | Escalate lên DEO (BR-ORG-016) |
| LEO-08 | ☐ | — | POST | `/v1/reports/{id}/dispatch-to-company` | Điều phối công ty |
| LEO-09 | ☐ | — | PUT | `/v1/reports/{id}/waste-tags` | Gắn waste tags |
| LEO-10 | ☐ | — | GET | `/v1/waste-tags` | Danh sách waste tags |
| LEO-11 | ☐ | — | GET | `/v1/reports/progress-board` | Bảng theo dõi tiến độ phường |
| LEO-12 | ☐ | — | GET | `/v1/reports/{id}/progress` | Chi tiết tiến độ báo cáo |

### 8.2 Duplicate & violation recurrence

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| LEO-13 | ☐ | — | GET | `/v1/reports/duplicate-candidates` | Danh sách nghi trùng (+ `media[]`) |
| LEO-14 | ☐ | — | POST | `/v1/reports/{id}/confirm-duplicate` | Xác nhận trùng |
| LEO-15 | ☐ | — | POST | `/v1/reports/{id}/dismiss-duplicate` | Bác bỏ cờ trùng |
| LEO-16 | ☐ | — | GET | `/v1/reports/violation-recurrence-candidates` | Nghi tái phạm (+ `media[]`) |
| LEO-17 | ☐ | — | GET | `/v1/reports/{id}/violation-recurrence-comparison` | So sánh báo cáo cũ/mới |
| LEO-18 | ☐ | — | POST | `/v1/reports/{id}/dismiss-violation-recurrence` | Bác bỏ cờ tái phạm |

### 8.3 Reopen requests

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| LEO-19 | ☐ | — | GET | `/v1/reports/reopen-requests` | Queue yêu cầu mở lại |
| LEO-20 | ☐ | — | POST | `/v1/reports/{id}/reopen-requests/{requestId}/approve` | Duyệt reopen |
| LEO-21 | ☐ | — | POST | `/v1/reports/{id}/reopen-requests/{requestId}/reject` | Từ chối reopen |

### 8.4 Inspection (LEO tạo & quản lý)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| LEO-22 | ☐ | — | POST | `/v1/reports/{id}/inspections` | Tạo inspection từ báo cáo |
| LEO-23 | ☐ | — | GET | `/v1/reports/{id}/inspections` | DS inspection của báo cáo |
| LEO-24 | ☐ | — | GET | `/v1/inspections/{id}` | Chi tiết inspection |
| LEO-25 | ☐ | — | PUT | `/v1/inspections/{id}/assign-team` | Gán team inspection |
| LEO-26 | ☐ | — | GET | `/v1/inspections/{id}/payments` | Xem payments |

### 8.5 Teams & staff (phường)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| LEO-27 | ☐ | — | GET | `/v1/teams` | Danh sách team trong scope |
| LEO-28 | ☐ | — | GET | `/v1/teams/{id}` | Chi tiết team |
| LEO-29 | ☐ | — | POST | `/v1/teams` | Tạo team |
| LEO-30 | ☐ | — | PUT | `/v1/teams/{id}` | Sửa team |
| LEO-31 | ☐ | — | POST | `/v1/teams/{teamId}/members` | Thêm thành viên |
| LEO-32 | ☐ | — | DELETE | `/v1/teams/{teamId}/members/{userId}` | Xóa thành viên |
| LEO-33 | ☐ | — | PUT | `/v1/teams/{teamId}/members/{userId}/transfer` | Chuyển team |
| LEO-34 | ☐ | — | GET | `/v1/offices/my/reports` | Báo cáo thuộc office |
| LEO-35 | ☐ | — | GET | `/v1/offices/my/staff` | Staff phường |
| LEO-36 | ☐ | — | GET | `/v1/offices/my/staff/lookup` | Tra user để mời |
| LEO-37 | ☐ | — | POST | `/v1/offices/my/staff` | Mời staff (gửi invitation) |
| LEO-38 | ☐ | — | DELETE | `/v1/offices/my/staff/{userId}` | Gỡ staff |

### 8.6 Community cleanup (LEO duyệt)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| LEO-39 | ☐ | — | POST | `/v1/reports/{reportId}/community-cleanups` | Tạo sự kiện |
| LEO-40 | ☐ | — | GET | `/v1/community-cleanups/office-queue` | Queue duyệt |
| LEO-41 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/verify` | Duyệt hoàn thành |
| LEO-42 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/reject-verification` | Từ chối |
| LEO-43 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/close-join` | Đóng đăng ký |
| LEO-44 | ☐ | — | POST | `/v1/community-cleanups/{eventId}/cancel` | Hủy sự kiện |

### 8.7 KPI, export, công ty phường

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| LEO-45 | ☐ | — | GET | `/v1/reports/officer-kpi` | KPI cá nhân |
| LEO-46 | ☐ | — | GET | `/v1/reports/export` | Export CSV/Excel phường |
| LEO-47 | ☐ | — | GET | `/v1/companies/my-ward` | Công ty phục vụ phường (dispatch) |
| LEO-48 | ☐ | — | GET | `/v1/offices` | Danh sách office (read) |
| LEO-49 | ☐ | — | GET | `/v1/offices/{id}` | Chi tiết office |
| LEO-50 | ☐ | — | POST | `/v1/comments/comments/{commentId}/hide` | Ẩn comment vi phạm |
| LEO-51 | ☐ | — | GET | `/v1/violating-entities` | Tra cứu violator (read/create) |

> **+** Auth, Profile, Notifications (Web).

---

## 9. DEO — Web

### 9.1 Department & offices

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| DEO-01 | ☐ | — | GET | `/v1/departments` | Danh sách sở |
| DEO-02 | ☐ | — | GET | `/v1/departments/{id}` | Chi tiết sở |
| DEO-03 | ☐ | — | GET | `/v1/departments/my-offices` | Office thuộc sở |
| DEO-04 | ☐ | — | GET | `/v1/departments/my/reports` | Báo cáo cấp tỉnh/TP |
| DEO-05 | ☐ | — | GET | `/v1/offices` | Danh sách phường |
| DEO-06 | ☐ | — | GET | `/v1/offices/{id}` | Chi tiết phường |

### 9.2 Companies (quản lý đối tác)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| DEO-07 | ☐ | — | POST | `/v1/companies` | Tạo công ty |
| DEO-08 | ☐ | — | GET | `/v1/companies` | Danh sách công ty |
| DEO-09 | ☐ | — | GET | `/v1/companies/{id}` | Chi tiết |
| DEO-10 | ☐ | — | POST | `/v1/companies/{id}/manager` | Gán CompanyManager |
| DEO-11 | ☐ | — | POST | `/v1/companies/{id}/manager/{userId}/reset-password` | Reset MK manager |
| DEO-12 | ☐ | — | POST | `/v1/companies/{id}/suspend` | Tạm ngưng |
| DEO-13 | ☐ | — | POST | `/v1/companies/{id}/terminate` | Chấm dứt HĐ |
| DEO-14 | ☐ | — | POST | `/v1/companies/{id}/reactivate` | Kích hoạt lại |
| DEO-15 | ☐ | — | DELETE | `/v1/companies/{id}` | Xóa công ty |
| DEO-16 | ☐ | — | GET | `/v1/companies/{id}/service-areas` | Vùng phục vụ |
| DEO-17 | ☐ | — | PUT | `/v1/companies/{id}/service-areas` | Cập nhật vùng |
| DEO-18 | ☐ | — | POST | `/v1/companies/{id}/renew-contract` | Gia hạn HĐ |
| DEO-19 | ☐ | — | GET | `/v1/companies/{id}/contract-history` | Lịch sử HĐ |
| DEO-20 | ☐ | — | GET | `/v1/companies/{id}/kpi` | KPI công ty |

### 9.3 Reports & teams (scope tỉnh)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| DEO-21 | ☐ | — | GET | `/v1/reports/queue` | Hàng đợi (escalated / tỉnh) |
| DEO-22 | ☐ | — | GET | `/v1/reports/{id}` | Chi tiết |
| DEO-23 | ☐ | — | GET | `/v1/reports/export` | Export toàn tỉnh |
| DEO-24 | ☐ | — | GET | `/v1/reports/officer-kpi` | KPI (officerId param) |
| DEO-25 | ☐ | — | GET | `/v1/teams` | Teams trong scope |
| DEO-26 | ☐ | — | GET | `/v1/teams/{id}` | Chi tiết team |
| DEO-27 | ☐ | — | POST | `/v1/comments/comments/{commentId}/hide` | Moderation |

> **+** Auth, Profile, Notifications. DEO **không** verify/assign trực tiếp trừ khi product mở rộng — hiện tại tập trung company + escalated queue.

---

## 10. CompanyManager — Web

### 10.1 Company profile & staff

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CMG-01 | ☐ | — | GET | `/v1/companies/my` | Thông tin công ty |
| CMG-02 | ☐ | — | GET | `/v1/companies/my/contract-history` | Lịch sử HĐ |
| CMG-03 | ☐ | — | GET | `/v1/companies/my/kpi` | KPI công ty |
| CMG-04 | ☐ | — | POST | `/v1/companies/my/staff` | Tạo staff |
| CMG-05 | ☐ | — | GET | `/v1/companies/my/staff` | Danh sách staff |
| CMG-06 | ☐ | — | PUT | `/v1/companies/my/staff/{userId}/status` | Kích hoạt/vô hiệu staff |

### 10.2 Company teams

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CMG-07 | ☐ | — | GET | `/v1/teams/company-teams` | Teams công ty |
| CMG-08 | ☐ | — | POST | `/v1/teams/company-teams` | Tạo team |
| CMG-09 | ☐ | — | PUT | `/v1/teams/company-teams/{id}` | Sửa team |
| CMG-10 | ☐ | — | PUT | `/v1/teams/company-teams/{id}/archive` | Lưu trữ |
| CMG-11 | ☐ | — | DELETE | `/v1/teams/company-teams/{id}` | Xóa team |
| CMG-12 | ☐ | — | POST | `/v1/teams/company-teams/{teamId}/members` | Thêm nhân viên |
| CMG-13 | ☐ | — | DELETE | `/v1/teams/company-teams/{teamId}/members/{userId}` | Gỡ nhân viên |
| CMG-14 | ☐ | — | GET | `/v1/teams/{id}` | Chi tiết team |

### 10.3 Task queue & assignment

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CMG-15 | ☐ | — | GET | `/v1/reports/company-queue` | Task chờ phân công |
| CMG-16 | ☐ | — | POST | `/v1/reports/{id}/assign-company-team` | Phân công team |
| CMG-17 | ☐ | — | GET | `/v1/reports/company-assignments` | Task đã phân công |
| CMG-18 | ☐ | — | GET | `/v1/reports/company-assignments/{reportId}` | Chi tiết tiến độ |
| CMG-19 | ☐ | — | GET | `/v1/reports/{id}/progress` | Board tiến độ |

### 10.4 Dashboard analytics

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| CMG-20 | ☐ | — | GET | `/v1/dashboard/company/overview` | Tổng quan |
| CMG-21 | ☐ | — | GET | `/v1/dashboard/company/workload-trend` | Xu hướng workload |
| CMG-22 | ☐ | — | GET | `/v1/dashboard/company/task-status` | Phân bố trạng thái |
| CMG-23 | ☐ | — | GET | `/v1/dashboard/company/team-performance` | Hiệu suất team |
| CMG-24 | ☐ | — | GET | `/v1/dashboard/company/staff-performance` | Hiệu suất staff |
| CMG-25 | ☐ | — | GET | `/v1/dashboard/company/queue-aging` | Tuổi hàng đợi |
| CMG-26 | ☐ | — | GET | `/v1/dashboard/company/recent-activities` | Hoạt động gần đây |
| CMG-27 | ☐ | — | GET | `/v1/dashboard/company/upcoming-deadlines` | Deadline sắp tới |

> **+** Auth, Profile, Notifications.

---

## 11. Admin — Web

### 11.1 User & role management

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-01 | ☐ | — | POST | `/v1/admin/users` | Tạo user |
| ADM-02 | ☐ | — | GET | `/v1/admin/users/all` | All users (no paging) |
| ADM-03 | ☐ | — | GET | `/v1/admin/users` | Users paginated |
| ADM-04 | ☐ | — | GET | `/v1/admin/users/{id}` | Chi tiết user |
| ADM-05 | ☐ | — | PUT | `/v1/admin/users/{id}` | Sửa user |
| ADM-06 | ☐ | — | DELETE | `/v1/admin/users/{id}` | Soft delete |
| ADM-07 | ☐ | — | PUT | `/v1/admin/users/{id}/role` | Đổi role |
| ADM-08 | ☐ | — | PUT | `/v1/admin/users/{id}/ban` | Ban/unban |

### 11.2 Reports moderation

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-09 | ☐ | — | GET | `/v1/admin/reports` | All reports |
| ADM-10 | ☐ | — | GET | `/v1/admin/reports/{id}` | Chi tiết |
| ADM-11 | ☐ | — | PUT | `/v1/admin/reports/{id}/status` | Override status |
| ADM-12 | ☐ | — | POST | `/v1/admin/reports/{id}/hide` | Ẩn báo cáo |
| ADM-13 | ☐ | — | POST | `/v1/admin/reports/{id}/unhide` | Bỏ ẩn |
| ADM-14 | ☐ | — | GET | `/v1/admin/spam-suspects` | Nghi spam |

### 11.3 Catalog admin

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-15 | ☐ | — | GET | `/v1/admin/pollution-categories` | CRUD categories |
| ADM-16 | ☐ | — | POST | `/v1/admin/pollution-categories` | Tạo |
| ADM-17 | ☐ | — | PUT | `/v1/admin/pollution-categories/{id}` | Sửa |
| ADM-18 | ☐ | — | DELETE | `/v1/admin/pollution-categories/{id}` | Xóa |
| ADM-19 | ☐ | — | PUT | `/v1/admin/pollution-categories/{id}/archive` | Archive |
| ADM-20 | ☐ | — | GET | `/v1/admin/waste-tags` | Waste tags |
| ADM-21 | ☐ | — | POST | `/v1/admin/waste-tags` | Tạo |
| ADM-22 | ☐ | — | PUT | `/v1/admin/waste-tags/{id}` | Sửa |
| ADM-23 | ☐ | — | PATCH | `/v1/admin/waste-tags/{id}/toggle` | Bật/tắt |
| ADM-24 | ☐ | — | DELETE | `/v1/admin/waste-tags/{id}` | Xóa |

### 11.4 RBAC, penalty, gamification config

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-25 | ☐ | — | GET | `/v1/admin/roles` | Roles |
| ADM-26 | ☐ | — | GET | `/v1/admin/permissions` | Permissions |
| ADM-27 | ☐ | — | GET | `/v1/admin/penalty-frameworks` | Khung xử phạt |
| ADM-28 | ☐ | — | POST | `/v1/admin/penalty-frameworks` | Tạo |
| ADM-29 | ☐ | — | PUT | `/v1/admin/penalty-frameworks/{id}` | Sửa |
| ADM-30 | ☐ | — | PATCH | `/v1/admin/penalty-frameworks/{id}/toggle` | Toggle |
| ADM-31 | ☐ | — | GET | `/v1/admin/gamification-configs` | Cấu hình điểm/badge |
| ADM-32 | ☐ | — | PUT | `/v1/admin/gamification-configs/{id}` | Cập nhật |
| ADM-33 | ☐ | — | POST | `/v1/gamification/{userId}/lock` | Khóa gamification user |

### 11.5 Audit & compliance

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-34 | ☐ | — | GET | `/v1/admin/audit-logs` | Audit logs |
| ADM-35 | ☐ | — | GET | `/v1/admin/audit-logs/export` | Export audit |
| ADM-36 | ☐ | — | GET | `/v1/admin/audit-logs/stats` | Thống kê |
| ADM-37 | ☐ | — | GET | `/v1/admin/audit-logs/{id}` | Chi tiết log |
| ADM-38 | ☐ | — | GET | `/v1/admin/blocked-words` | Từ cấm |
| ADM-39 | ☐ | — | POST | `/v1/admin/blocked-words` | Thêm |
| ADM-40 | ☐ | — | PUT | `/v1/admin/blocked-words/{id}` | Sửa |
| ADM-41 | ☐ | — | DELETE | `/v1/admin/blocked-words/{id}` | Xóa |

### 11.6 Notification templates

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-42 | ☐ | — | GET | `/v1/admin/notification-templates` | Templates |
| ADM-43 | ☐ | — | POST | `/v1/admin/notification-templates` | Tạo |
| ADM-44 | ☐ | — | GET | `/v1/admin/notification-templates/{id}` | Chi tiết |
| ADM-45 | ☐ | — | PUT | `/v1/admin/notification-templates/{id}` | Sửa |
| ADM-46 | ☐ | — | DELETE | `/v1/admin/notification-templates/{id}` | Xóa |
| ADM-47 | ☐ | — | PATCH | `/v1/admin/notification-templates/{id}/publish` | Publish |
| ADM-48 | ☐ | — | POST | `/v1/admin/notification-templates/{id}/test` | Gửi test |

### 11.7 Admin dashboard

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-49 | ☐ | — | GET | `/v1/dashboard/admin/overview` | Tổng quan hệ thống |
| ADM-50 | ☐ | — | GET | `/v1/dashboard/admin/report-status` | Phân bố status |
| ADM-51 | ☐ | — | GET | `/v1/dashboard/admin/report-trend` | Xu hướng báo cáo |
| ADM-52 | ☐ | — | GET | `/v1/dashboard/admin/pollution-analytics` | Phân tích loại ô nhiễm |
| ADM-53 | ☐ | — | GET | `/v1/dashboard/admin/geographic` | Phân bố địa lý |
| ADM-54 | ☐ | — | GET | `/v1/dashboard/admin/report-funnel` | Funnel |
| ADM-55 | ☐ | — | GET | `/v1/dashboard/admin/company-performance` | Hiệu suất công ty |
| ADM-56 | ☐ | — | GET | `/v1/dashboard/admin/officer-performance` | Hiệu suất officer |
| ADM-57 | ☐ | — | GET | `/v1/dashboard/admin/queue-aging` | Tuổi queue |
| ADM-58 | ☐ | — | GET | `/v1/dashboard/admin/resolution-distribution` | Phân bố resolution |
| ADM-59 | ☐ | — | GET | `/v1/dashboard/admin/recent-activities` | Hoạt động |
| ADM-60 | ☐ | — | GET | `/v1/dashboard/admin/alerts` | Cảnh báo hệ thống |

### 11.8 Admin-only org (departments / offices CRUD)

| ID | ☐ Web | ☐ Mobile | Method | Endpoint | Mô tả |
|----|-------|----------|--------|----------|-------|
| ADM-61 | ☐ | — | POST | `/v1/departments` | Tạo sở |
| ADM-62 | ☐ | — | PUT | `/v1/departments/{id}` | Sửa sở |
| ADM-63 | ☐ | — | DELETE | `/v1/departments/{id}` | Xóa sở |
| ADM-64 | ☐ | — | PUT | `/v1/departments/{id}/officer` | Gán DEO |
| ADM-65 | ☐ | — | POST | `/v1/offices` | Tạo phường |
| ADM-66 | ☐ | — | PUT | `/v1/offices/{id}` | Sửa phường |
| ADM-67 | ☐ | — | PUT | `/v1/offices/{id}/officer` | Gán LEO |

> Admin có thể gọi thêm hầu hết endpoint LEO/DEO/CompanyManager (role `Admin`).

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
| 2026-07-30 | Tạo checklist ban đầu từ 20 controllers; bổ sung `violation-recurrence-candidates`, `media[]` trên candidate APIs |

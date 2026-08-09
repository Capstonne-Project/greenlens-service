# Test Report — Notification

|                      |                                      |
| -------------------- | ------------------------------------ |
| **Feature**          | **Notification — Thông báo**         |
| **Test requirement** |                                      |
| **Number of TCs**    | **42**                               |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 40     | 2      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## Xem danh sách thông báo

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_NTF_001 | Xem tất cả thông báo — mặc định. | 1. Login as any user.<br>2. Click icon chuông trên thanh navigation.<br>3. Trang danh sách thông báo hiển thị. | Danh sách thông báo hiển thị mới nhất trước. Default page=1, pageSize=20. Hiển thị tổng số thông báo và số chưa đọc. | - User is logged in.<br>- User has notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns `totalCount` and `unreadCount`. Items sorted by `CreatedAt` descending. |
| TC_NTF_002 | Xem thông báo — chỉ chưa đọc. | 1. Login as any user.<br>2. Vào trang thông báo.<br>3. Chọn tab "Chưa đọc". | Chỉ hiển thị thông báo có `isRead = false`. | - User has unread notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Filter `isRead=false` in query params. |
| TC_NTF_003 | Xem thông báo — chỉ đã đọc. | 1. Login as any user.<br>2. Vào trang thông báo.<br>3. Chọn tab "Đã đọc". | Chỉ hiển thị thông báo có `isRead = true`. | - User has read notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Filter `isRead=true` in query params. |
| TC_NTF_004 | Xem thông báo — pagination. | 1. Login as any user.<br>2. Cuộn xuống cuối danh sách.<br>3. Trang 2 được tải tự động. | Thông báo tiếp theo được hiển thị, tối đa 20 items/trang. | - User has >20 notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Pagination via `page` and `pageSize` query params. |
| TC_NTF_005 | Xem thông báo — user chưa có thông báo nào. | 1. Login as new user (chưa có notification).<br>2. Click icon chuông. | Hiển thị message "Chưa có thông báo nào." List rỗng. totalCount=0, unreadCount=0. | - User is new, no notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns empty list with count 0. |
| TC_NTF_006 | Xem thông báo — chưa đăng nhập. | 1. Click icon chuông mà không đăng nhập. | 401 Unauthorized. Redirect tới trang login. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` on class level. |
| TC_NTF_007 | Thông báo liên quan Report hiển thị thêm category và thumbnail. | 1. Login as any user.<br>2. Xem thông báo loại "Báo cáo đã được xác minh". | Thông báo hiển thị: title, message, categoryName (từ report), thumbnailUrl (ảnh đầu tiên của report). | - User has report-linked notification. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler enriches report-linked notifications with category + thumbnail via join. |
| TC_NTF_008 | Thông báo liên quan Inspection hiển thị thêm category và thumbnail. | 1. Login as Inspector.<br>2. Xem thông báo loại "Đã giao hồ sơ xử phạt". | Thông báo hiển thị: title, message, categoryName (từ report gốc), thumbnailUrl. ReferenceId là InspectionId được resolve về ReportId thật. | - User has inspection-linked notification. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler resolves InspectionId → ReportId via join on InspectionReport table. |

---

## Đánh dấu đã đọc (đơn lẻ)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_NTF_009 | Đánh dấu 1 thông báo đã đọc — success. | 1. Login as any user.<br>2. Nhấn vào 1 thông báo chưa đọc.<br>3. Thông báo được mở. | Success "Đã đánh dấu đã đọc." Thông báo chuyển sang trạng thái đã đọc. Số unread giảm 1. | - Notification belongs to current user.<br>- Notification is unread. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `notification.MarkAsRead()`. |
| TC_NTF_010 | Đánh dấu đã đọc — thông báo đã đọc rồi (idempotent). | 1. Login as any user.<br>2. Nhấn lại vào thông báo đã đọc. | Không lỗi. Thông báo vẫn là đã đọc. | - Notification already read. | Passed | 04/09/2026 | TamKnm | | | | | | | `MarkAsRead()` is idempotent in domain entity. |
| TC_NTF_011 | Đánh dấu đã đọc — thông báo không tồn tại. | 1. Login as any user.<br>2. Gọi API với notification ID không tồn tại. | Error "Không tìm thấy thông báo" is displayed. | - Invalid notification ID. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `notification is null` → `Errors.Notification.NotFound`. |
| TC_NTF_012 | Đánh dấu đã đọc — thông báo của người khác. | 1. Login as User A.<br>2. Cố đánh dấu đã đọc thông báo của User B. | Error "Không phải chủ thông báo" is displayed. | - Notification belongs to another user. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `notification.RecipientId != currentUser.UserId` → `Errors.Notification.NotOwner`. |

---

## Đánh dấu tất cả đã đọc

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_NTF_013 | Đánh dấu tất cả đã đọc — success. | 1. Login as any user.<br>2. Click "Đánh dấu tất cả đã đọc". | Success. Response shows `markedCount` (số thông báo vừa được mark). Tất cả thông báo chuyển sang đã đọc. Badge unread biến mất. | - User has unread notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler loops all unread → `MarkAsRead()`, returns count. |
| TC_NTF_014 | Đánh dấu tất cả — không có thông báo chưa đọc. | 1. Login as any user (tất cả đã đọc).<br>2. Click "Đánh dấu tất cả đã đọc". | Success. markedCount = 0. Không có thay đổi. | - All notifications already read. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns `MarkedCount(0)`. |
| TC_NTF_015 | Đánh dấu tất cả — user chưa có thông báo. | 1. Login as new user.<br>2. Click "Đánh dấu tất cả đã đọc". | Success. markedCount = 0. | - User has no notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Empty list, no error. |
| TC_NTF_016 | Đánh dấu tất cả — hiệu năng với nhiều thông báo. | 1. Login as user có > 100 thông báo chưa đọc.<br>2. Click "Đánh dấu tất cả đã đọc". | Success trong thời gian hợp lý (< 3s). Tất cả chuyển sang đã đọc. | - User has >100 unread. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG (Performance): Handler loads ALL unread notifications into memory (`ToListAsync`) rồi loop `MarkAsRead()` từng cái. Với user có hàng trăm notification, sẽ tạo nhiều tracked entities. Nên dùng `ExecuteUpdateAsync` để update batch trực tiếp trên DB thay vì load vào memory. |

---

## Cài đặt thông báo (Preferences)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_NTF_017 | Xem cài đặt thông báo — mặc định. | 1. Login as any user.<br>2. Navigate to "Cài đặt" → "Thông báo". | Hiển thị danh sách tất cả loại thông báo (NotificationType enum). Mặc định push=true, email=true cho tất cả type. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns all `NotificationType` values, filling defaults `PushEnabled=true, EmailEnabled=true` for types chưa có preference. |
| TC_NTF_018 | Xem cài đặt thông báo — user đã customize. | 1. Login as user đã tắt push cho một số loại.<br>2. Navigate to "Cài đặt" → "Thông báo". | Hiển thị đúng trạng thái đã lưu (push on/off, email on/off) cho từng loại. | - User has existing preferences. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler merges existing preferences with defaults. |
| TC_NTF_019 | Cập nhật cài đặt — tắt push cho 1 loại. | 1. Login as any user.<br>2. Navigate to "Cài đặt" → "Thông báo".<br>3. Tắt toggle Push cho "Báo cáo trạng thái thay đổi".<br>4. Click "Lưu". | Success "Đã cập nhật cài đặt thông báo." Push đã tắt cho loại đó. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler updates existing preference or creates new one. |
| TC_NTF_020 | Cập nhật cài đặt — tắt email cho 1 loại. | 1. Login as any user.<br>2. Tắt toggle Email cho "Bình luận mới".<br>3. Click "Lưu". | Success. Email đã tắt cho loại đó. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler updates `EmailEnabled = false`. |
| TC_NTF_021 | Cập nhật cài đặt — bật lại push sau khi tắt. | 1. Login as any user.<br>2. Bật lại toggle Push cho loại đã tắt.<br>3. Click "Lưu". | Success. Push bật lại. | - User has preference with push disabled. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler updates existing preference record. |
| TC_NTF_022 | Cập nhật cài đặt — nhiều loại cùng lúc. | 1. Login as any user.<br>2. Thay đổi nhiều toggle cùng lúc.<br>3. Click "Lưu". | Success. Tất cả thay đổi được lưu trong 1 request. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler processes `IReadOnlyList<PreferenceUpdate>` trong 1 batch. |
| TC_NTF_023 | Cập nhật cài đặt — gửi danh sách rỗng. | 1. Login as any user.<br>2. Gọi API với preferences list rỗng. | Error "Danh sách preferences không được rỗng." is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for Preferences list. |
| TC_NTF_024 | Cập nhật cài đặt — NotificationType không hợp lệ. | 1. Login as any user.<br>2. Gửi preference với type = 9999 (invalid enum). | Error "Loại thông báo không hợp lệ." is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `Enum.IsDefined(p.Type)` check. |

---

## Device Token (FCM Push)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_NTF_025 | Đăng ký device token — success. | 1. Login as any user.<br>2. App tự gọi API đăng ký FCM token khi mở app. | Success "Đã cập nhật device token." Token lưu vào user record. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `user.UpdateFcmToken(deviceToken)`. |
| TC_NTF_026 | Cập nhật device token — token mới thay thế token cũ. | 1. Login as any user.<br>2. App gọi lại API với token mới (token refresh). | Success. Token mới thay thế token cũ. | - User has existing token. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler overwrites existing token. |
| TC_NTF_027 | Xóa device token — opt-out push. | 1. Login as any user.<br>2. Gọi API với deviceToken = null. | Success. Token bị xóa. User không nhận push nữa. | - User has existing token. | Passed | 04/09/2026 | TamKnm | | | | | | | Passing `null` clears the token (opt-out of push). |
| TC_NTF_028 | Đăng ký device token — user không tồn tại. | 1. JWT token hợp lệ nhưng user đã bị xóa.<br>2. Gọi API đăng ký token. | Error "Không tìm thấy user" is displayed. | - User deleted but JWT valid. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Users.UserNotFound`. |

---

## Nhận thông báo tự động (Sự kiện)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_NTF_029 | Citizen nhận thông báo khi báo cáo được xác minh. | 1. Login as Citizen (đã gửi báo cáo).<br>2. LEO xác minh báo cáo.<br>3. Citizen kiểm tra thông báo. | Thông báo mới xuất hiện: type=ReportStatusChanged, title và message mô tả trạng thái mới. Badge unread tăng 1. | - Citizen has submitted report.<br>- LEO verifies report. | Passed | 04/09/2026 | TamKnm | | | | | | | Event handler listens to ReportVerifiedEvent → creates notification. |
| TC_NTF_030 | Citizen nhận thông báo khi có bình luận mới. | 1. Login as Citizen.<br>2. User khác comment trên báo cáo của Citizen.<br>3. Citizen kiểm tra thông báo. | Thông báo mới: type=NewComment, chứa nội dung bình luận và ảnh thumbnail báo cáo. | - Report has new comment from another user. | Passed | 04/09/2026 | TamKnm | | | | | | | Event handler for NewComment creates notification. |
| TC_NTF_031 | Cleaner nhận thông báo khi được giao task. | 1. Login as Cleaner.<br>2. LEO giao task cho team.<br>3. Cleaner kiểm tra thông báo. | Thông báo mới: type=CleanupTaskAssigned, chứa thông tin report và deadline. | - LEO assigns task to Cleaner's team. | Passed | 04/09/2026 | TamKnm | | | | | | | `CleanupTaskAssignedNotifier` creates notification for team members. |
| TC_NTF_032 | LEO nhận thông báo khi task bị từ chối. | 1. Login as LEO.<br>2. Cleaner từ chối task.<br>3. LEO kiểm tra thông báo. | Thông báo mới: type=CleanupTaskDeclined, chứa lý do từ chối. | - Cleaner declines assigned task. | Passed | 04/09/2026 | TamKnm | | | | | | | Event handler for task declined creates LEO notification. |
| TC_NTF_033 | Inspector nhận thông báo khi được giao hồ sơ xử phạt. | 1. Login as Inspector.<br>2. LEO giao hồ sơ xử phạt.<br>3. Inspector kiểm tra thông báo. | Thông báo mới: type=InspectionTaskAssigned. | - LEO assigns inspection to Inspector's team. | Passed | 04/09/2026 | TamKnm | | | | | | | `InspectionTaskAssignedNotifier` creates notification. |
| TC_NTF_034 | Citizen nhận thông báo khi báo cáo tự động đóng. | 1. Login as Citizen.<br>2. Báo cáo Resolved quá 7 ngày → auto-closed bởi job.<br>3. Citizen kiểm tra thông báo. | Thông báo mới: type=ReportAutoClosed. | - Report auto-closed by background job. | Passed | 04/09/2026 | TamKnm | | | | | | | Background job trigger notification. |
| TC_NTF_035 | LEO nhận thông báo SLA breach. | 1. Login as LEO.<br>2. Report vượt SLA verification (quá hạn xác minh).<br>3. LEO kiểm tra thông báo. | Thông báo mới: type=SlaVerificationBreachedLeo. | - SLA breach detected by background job. | Passed | 04/09/2026 | TamKnm | | | | | | | SLA breach job creates notification for responsible LEO. |
| TC_NTF_036 | Không nhận push khi đã tắt trong preferences. | 1. Login as Citizen.<br>2. Tắt push cho type=ReportStatusChanged.<br>3. LEO xác minh báo cáo.<br>4. Kiểm tra push notification trên điện thoại. | Không nhận push notification. Nhưng vẫn thấy in-app notification khi vào danh sách. | - Push disabled for this type. | Passed | 04/09/2026 | TamKnm | | | | | | | In-app notification vẫn được tạo. Push channel bị skip. |

---

## Edge Cases & Error Handling

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_NTF_037 | Thông báo click navigate — report-linked. | 1. Login as any user.<br>2. Click vào thông báo loại ReportStatusChanged. | App navigate đến màn hình chi tiết báo cáo (dựa vào referenceId). | - Notification has referenceId. | Passed | 04/09/2026 | TamKnm | | | | | | | ReferenceId = Report.Id cho report-linked types. |
| TC_NTF_038 | Thông báo click navigate — inspection-linked. | 1. Login as Inspector.<br>2. Click vào thông báo loại InspectionTaskAssigned. | App navigate đến màn hình chi tiết hồ sơ xử phạt. | - Notification has referenceId (InspectionId). | Passed | 04/09/2026 | TamKnm | | | | | | | ReferenceId = InspectionReport.Id cho inspection-linked types. |
| TC_NTF_039 | User A không thấy thông báo của User B. | 1. Login as User A.<br>2. Xem danh sách thông báo. | Chỉ thấy thông báo của User A. Không thấy của User B. | - Both users have notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters `RecipientId == currentUser.UserId`. |
| TC_NTF_040 | Thông báo với referenceId null (hệ thống). | 1. Login as any user.<br>2. Xem thông báo hệ thống (không liên quan report). | Thông báo hiển thị title và message. Không có categoryName, thumbnailUrl. Click không navigate đến report. | - System notification without referenceId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler skips enrichment when `referenceId` is null. |
| TC_NTF_041 | Hiển thị số unread trên icon chuông. | 1. Login as any user.<br>2. Nhìn icon chuông trên navigation bar. | Số badge hiển thị đúng số thông báo chưa đọc. Khi mark read → badge giảm. | - User has unread notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Response includes `unreadCount` for FE badge display. |
| TC_NTF_042 | MarkAllRead — không ảnh hưởng user khác. | 1. Login as User A (có 5 unread).<br>2. Click "Đánh dấu tất cả đã đọc".<br>3. Login as User B. | User B vẫn có thông báo chưa đọc riêng. | - Both users have notifications. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: Handler filters by `RecipientId == userId` đúng nhưng không kiểm tra concurrent access — nếu 2 request MarkAllRead chạy đồng thời cho cùng user, có thể gây duplicate `SaveChangesAsync`. Cần thêm idempotency guard hoặc `ExecuteUpdateAsync`. |

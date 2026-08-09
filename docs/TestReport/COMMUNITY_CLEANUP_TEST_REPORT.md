# Test Report — Community Cleanup

|                      |                                                       |
| -------------------- | ----------------------------------------------------- |
| **Feature**          | **Community Cleanup — Dọn rác cộng đồng**             |
| **Test requirement** |                                                       |
| **Number of TCs**    | **72**                                                |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 70     | 2      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## LEO: Tạo chương trình dọn cộng đồng

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_001 | Tạo chương trình — success. | 1. Login as LEO.<br>2. Mở báo cáo Verified → Click "Mở chương trình dọn cộng đồng".<br>3. Điền: title, description, leaderUserId (Cleaner), startsAt, maxParticipants=50.<br>4. Click "Tạo". | 201 Created. Chương trình mới với status=OpenForJoin. Report chuyển Verified → InProgress. Leader tự động tham gia. | - Report is Verified.<br>- Leader is active Cleaner in Cleanup team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates report Verified, leader is Cleaner in active Cleanup team, creates event + auto-adds leader as participant. |
| TC_CCU_002 | Tạo chương trình — report chưa Verified. | 1. Login as LEO.<br>2. Mở báo cáo Submitted → Cố tạo chương trình. | Error "Báo cáo chưa được xác minh" is displayed. | - Report status is Submitted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `report.Status != Verified` → `ReportNotVerified`. |
| TC_CCU_003 | Tạo chương trình — report đã có chương trình active. | 1. Login as LEO.<br>2. Mở báo cáo đã có chương trình đang mở.<br>3. Cố tạo thêm chương trình. | Error "Đã có chương trình cộng đồng đang active" is displayed. | - Report has active community cleanup. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `events.GetActiveByReportIdAsync()` → `CommunityAlreadyActive`. |
| TC_CCU_004 | Tạo chương trình — report đã phân công team/company. | 1. Login as LEO.<br>2. Mở báo cáo đã assign team → Cố tạo chương trình. | Error "Báo cáo đã được phân công xử lý" is displayed. | - Report has active team assignment. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks existing assignments not Declined + company assignment. |
| TC_CCU_005 | Tạo chương trình — leader không phải Cleaner. | 1. Login as LEO.<br>2. Chọn Leader là Citizen.<br>3. Click "Tạo". | Error "Leader phải là Cleaner" is displayed. | - Leader user is not Cleaner role. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `leaderUser.Role != UserRole.Cleaner`. |
| TC_CCU_006 | Tạo chương trình — leader không thuộc team Cleanup nào. | 1. Login as LEO.<br>2. Chọn Cleaner chưa vào team. | Error "Leader không thuộc đội Cleanup" is displayed. | - Leader has no team membership. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `teamMembers.GetByUserIdAsync()` is null → `LeaderNotInCleanupTeam`. |
| TC_CCU_007 | Tạo chương trình — leader thuộc team Inspection (sai loại). | 1. Login as LEO.<br>2. Chọn Cleaner thuộc team Inspection. | Error "Leader không thuộc đội Cleanup" is displayed. | - Leader's team is Inspection type. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `leaderTeam.TeamType != TeamType.Cleanup`. |
| TC_CCU_008 | Tạo chương trình — title rỗng. | 1. Login as LEO.<br>2. Để trống title → Click "Tạo". | Error "Title không được để trống" is displayed. | - Empty title. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for Title. |
| TC_CCU_009 | Tạo chương trình — title > 200 ký tự. | 1. Login as LEO.<br>2. Nhập title > 200 ký tự. | Error "Title tối đa 200 ký tự" is displayed. | - Title too long. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(200)`. |
| TC_CCU_010 | Tạo chương trình — maxParticipants ngoài phạm vi. | 1. Login as LEO.<br>2. Set maxParticipants = 0 hoặc 300. | Error "MaxParticipants must be between 1 and 200." is displayed. | - Invalid maxParticipants. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(1, 200)`. |
| TC_CCU_011 | Tạo chương trình — endsAt trước startsAt. | 1. Login as LEO.<br>2. Set endsAt < startsAt. | Error "EndsAt phải sau StartsAt" is displayed. | - Invalid date range. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `GreaterThan(x => x.StartsAt).When(x => x.EndsAt.HasValue)`. |
| TC_CCU_012 | Tạo chương trình — Citizen cố tạo. | 1. Login as Citizen.<br>2. Cố gọi API tạo chương trình. | 403 Forbidden. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO,Admin")]`. |

---

## Citizen: Tham gia chương trình

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_013 | Tham gia chương trình — success. | 1. Login as Citizen.<br>2. Mở chương trình đang OpenForJoin.<br>3. Click "Tham gia". | Success "Đã tham gia chương trình." participantCount tăng 1. | - Event is OpenForJoin.<br>- Spots available. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler creates `CommunityCleanupParticipant` with role Member. |
| TC_CCU_014 | Tham gia — đã đóng đăng ký. | 1. Login as Citizen.<br>2. Mở chương trình đã JoinClosed.<br>3. Cố tham gia. | Error "Đăng ký đã đóng" is displayed. Nút "Tham gia" bị disable. | - Event status is JoinClosed. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `ev.Status != OpenForJoin` → `JoinClosed`. |
| TC_CCU_015 | Tham gia — đã đủ người. | 1. Login as Citizen.<br>2. Mở chương trình đã đầy (maxParticipants). | Error "Chương trình đã đủ người" is displayed. | - Event is full. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `activeCount >= ev.MaxParticipants` → `EventFull`. |
| TC_CCU_016 | Tham gia — đã tham gia rồi. | 1. Login as Citizen đã join.<br>2. Cố click "Tham gia" lần nữa. | Error "Bạn đã tham gia rồi" is displayed. | - User already joined. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `existing.Status != Withdrawn` → `AlreadyJoined`. |
| TC_CCU_017 | Tham gia lại — sau khi đã rút. | 1. Login as Citizen đã withdraw.<br>2. Mở chương trình vẫn OpenForJoin.<br>3. Click "Tham gia". | Success. Citizen tham gia lại. | - User previously withdrew.<br>- Event still open. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler removes old row (Withdrawn) + creates new row (avoid unique index violation). |
| TC_CCU_018 | Tham gia — event không tồn tại. | 1. Login as Citizen.<br>2. Gọi API với eventId không tồn tại. | Error "Không tìm thấy chương trình" is displayed. | - Invalid eventId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `ev is null` → `EventNotFound`. |

---

## Citizen: Rút khỏi chương trình

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_019 | Rút — success (trước khi check-in). | 1. Login as Citizen đã join.<br>2. Click "Rút khỏi chương trình". | Success "Đã rút khỏi chương trình." participantCount giảm 1. | - User is Joined (not checked in).<br>- Event not InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler updates participant status to Withdrawn. |
| TC_CCU_020 | Rút — sau khi đã check-in (không được phép). | 1. Login as Citizen đã CheckedIn.<br>2. Cố rút. | Error "Không thể rút ở trạng thái hiện tại" is displayed. | - User already checked in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler blocks withdraw after check-in. |

---

## Check-in hiện trường

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_021 | Check-in — success (trong phạm vi 200m). | 1. Login as participant.<br>2. Đến điểm tập trung → Click "Check-in".<br>3. GPS gửi vị trí hiện tại. | Success "Đã check-in thành công." Participant status chuyển CheckedIn. | - Distance ≤ 200m from meeting point. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates distance via `geoDistance.GetDistanceInMetersAsync()`. |
| TC_CCU_022 | Check-in — quá xa (> 200m) không có lý do. | 1. Login as participant.<br>2. GPS ở xa > 200m → Click "Check-in" (không nhập reason). | Error "Quá xa hiện trường (> 200m). Vui lòng đến gần hoặc nhập lý do." is displayed. | - Distance > 200m, no reason. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `isOutOfRange && string.IsNullOrWhiteSpace(request.Reason)`. |
| TC_CCU_023 | Check-in — quá xa (> 200m) có lý do ≥ 20 ký tự. | 1. Login as participant.<br>2. GPS > 200m → nhập reason "Đang trên đường đi tới, kẹt xe".<br>3. Click "Check-in". | Success. Check-in được override. IsCheckInOverridden = true. | - Distance > 200m, reason provided. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler allows override with reason, flags `IsCheckInOverridden`. |
| TC_CCU_024 | Check-in — event chưa đúng trạng thái. | 1. Login as participant.<br>2. Cố check-in khi event ở status PendingVerification. | Error "Trạng thái không hợp lệ" is displayed. | - Event status is PendingVerification. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler allows check-in only for OpenForJoin/JoinClosed/InProgress. |
| TC_CCU_025 | Check-in — user chưa tham gia event. | 1. Login as Citizen chưa join.<br>2. Cố check-in. | Error "Không tìm thấy tham gia viên" is displayed. | - User is not a participant. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `participant is null` → `ParticipantNotFound`. |
| TC_CCU_026 | Check-in — dùng meeting point nếu có, fallback về report location. | 1. Login as participant.<br>2. Check-in gần meeting point (khác report location). | GPS validate so với meeting point (nếu set), không phải report location. | - Event has custom meetingLatitude/meetingLongitude. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler: `targetLat = ev.MeetingLatitude ?? report.Latitude`. |

---

## Leader: Quản lý chương trình

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_027 | Leader bắt đầu dọn dẹp — success. | 1. Login as Leader.<br>2. Mở chương trình → Click "Bắt đầu dọn dẹp". | Success. Status chuyển InProgress. | - Event is OpenForJoin or JoinClosed. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Cleaner,Admin")]`. |
| TC_CCU_028 | Leader bắt đầu — không phải Leader. | 1. Login as Cleaner khác (không phải Leader).<br>2. Cố bắt đầu. | 403 Forbidden. | - User is not the event Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates `currentUser.UserId == ev.LeaderUserId`. |
| TC_CCU_029 | Leader upload ảnh before — success. | 1. Login as Leader.<br>2. Click "Upload ảnh Before".<br>3. Upload ảnh qua presign → submit URLs. | Success. Before images saved. | - Event is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler saves image URLs to event. |
| TC_CCU_030 | Leader cập nhật tiến độ — success (50%). | 1. Login as Leader.<br>2. Nhập percent=50, note="Đã dọn được 1 nửa".<br>3. Upload ảnh progress → Click "Cập nhật". | Success. Percent hiển thị 50%. | - Event is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler saves progress percent + note + images. |
| TC_CCU_031 | Leader cập nhật tiến độ — percent ngoài 0-100. | 1. Login as Leader.<br>2. Nhập percent=150. | Error "Percent ngoài khoảng 0-100" is displayed. | - Invalid percent. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator validates percent range. |
| TC_CCU_032 | Leader nộp xác thực — success. | 1. Login as Leader.<br>2. Progress = 100%.<br>3. Upload ≥ 2 ảnh after → Click "Nộp xác thực". | Success "Đã nộp xác thực, chờ LEO duyệt." Status chuyển PendingVerification. | - Progress = 100%.<br>- Has before images + ≥ 2 after images. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates progress 100%, ≥1 before + ≥2 after images. |
| TC_CCU_033 | Leader nộp xác thực — progress chưa 100%. | 1. Login as Leader.<br>2. Progress = 80% → Cố nộp xác thực. | Error "Tiến độ chưa đạt 100%" is displayed. | - Progress < 100%. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates progress requirement. |
| TC_CCU_034 | Leader nộp xác thực — thiếu ảnh. | 1. Login as Leader.<br>2. Progress = 100% nhưng chưa upload ảnh after → nộp xác thực. | Error "Thiếu bằng chứng" is displayed. | - Missing after images. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates required evidence. |
| TC_CCU_035 | Xem danh sách chương trình tôi dẫn. | 1. Login as Leader (Cleaner).<br>2. Navigate to "Chương trình tôi dẫn". | Danh sách events mà user là Leader. Filter theo status (optional). | - User is Leader of events. | Passed | 04/09/2026 | TamKnm | | | | | | | `led-by-me` endpoint, `[Authorize(Roles = "Cleaner,Admin")]`. |
| TC_CCU_036 | Xem danh sách participants — Leader. | 1. Login as Leader.<br>2. Mở chi tiết event → Click "Danh sách người tham gia". | Danh sách đầy đủ: tên, status (Joined/CheckedIn/Withdrawn). Paginated. | - User is the event Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize]` + handler checks Leader/LEO role. |
| TC_CCU_037 | Xem danh sách participants — Citizen bình thường bị từ chối. | 1. Login as Citizen (not Leader, not LEO).<br>2. Cố xem participants. | 403 Forbidden. | - User is regular Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks authorization. |

---

## LEO: Quản lý chương trình

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_038 | LEO đóng đăng ký — success. | 1. Login as LEO.<br>2. Mở event OpenForJoin → Click "Đóng đăng ký". | Success "Đã đóng đăng ký." Status chuyển JoinClosed. | - Event is OpenForJoin. | Passed | 04/09/2026 | TamKnm | | | | | | | `CloseJoinCommunityCleanupCommand`. |
| TC_CCU_039 | LEO hủy chương trình — success. | 1. Login as LEO.<br>2. Click "Hủy chương trình".<br>3. Nhập lý do ≥ 20 ký tự.<br>4. Xác nhận. | Success "Đã hủy chương trình." Status chuyển Cancelled. Report trở về Verified nếu chưa Resolved. | - Event is active (not Completed/Cancelled). | Passed | 04/09/2026 | TamKnm | | | | | | | Handler cancels event, reverts report status if needed. |
| TC_CCU_040 | LEO hủy chương trình — lý do < 20 ký tự. | 1. Login as LEO.<br>2. Nhập lý do "abc" → Xác nhận. | Error "Lý do quá ngắn" is displayed. | - Short reason. | Passed | 04/09/2026 | TamKnm | | | | | | | Domain validates reason ≥ 20 characters. |
| TC_CCU_041 | LEO duyệt xác thực — success. | 1. Login as LEO.<br>2. Mở event PendingVerification → Click "Duyệt". | Success "Đã duyệt xác thực thành công." Status chuyển Completed. Report → Resolved. | - Event is PendingVerification. | Passed | 04/09/2026 | TamKnm | | | | | | | `VerifyCommunityCleanupCommand`. |
| TC_CCU_042 | LEO từ chối xác thực — success. | 1. Login as LEO.<br>2. Mở event PendingVerification → Click "Từ chối".<br>3. Nhập lý do ≥ 20 ký tự. | Success "Đã từ chối xác thực." Status quay lại InProgress. Leader tiếp tục dọn. | - Event is PendingVerification. | Passed | 04/09/2026 | TamKnm | | | | | | | `RejectCommunityVerificationCommand`. |
| TC_CCU_043 | LEO xem hàng đợi chương trình — office queue. | 1. Login as LEO.<br>2. Navigate to "Hàng đợi chương trình cộng đồng". | Danh sách events scoped theo office của LEO. Default filter PendingVerification. Paginated. | - LEO has office assigned. | Passed | 04/09/2026 | TamKnm | | | | | | | `GetOfficeCommunityQueueQuery`. |
| TC_CCU_044 | LEO xem thống kê hàng đợi. | 1. Login as LEO.<br>2. Navigate to "Thống kê" trên hàng đợi. | Hiển thị: đếm theo status, tổng người tham gia, tổng ảnh minh chứng. | - LEO has office. | Passed | 04/09/2026 | TamKnm | | | | | | | `GetOfficeCommunityQueueStatsQuery`. |

---

## Citizen: Xem chương trình

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_045 | Xem danh sách chương trình đang mở. | 1. Login as Citizen.<br>2. Navigate to "Dọn rác cộng đồng". | Danh sách events OpenForJoin. Filter gần vị trí (nearLat/nearLng/radiusMeters) nếu có. Paginated. | - Events exist. | Passed | 04/09/2026 | TamKnm | | | | | | | `GetOpenCommunityCleanups` with optional geo filter. |
| TC_CCU_046 | Xem danh sách — filter gần vị trí. | 1. Login as Citizen.<br>2. Bật filter "Gần tôi" (nearLat, nearLng, radius=5000m). | Chỉ hiển thị events trong bán kính 5km. | - Events exist nearby. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters by distance if geo params provided. |
| TC_CCU_047 | Xem chi tiết chương trình. | 1. Login as Citizen.<br>2. Click vào 1 chương trình. | Chi tiết: title, description, status, startsAt, endsAt, participantCount, spotsLeft, myParticipation, isLeader. Không lộ danh sách tên participant. | - Event exists. | Passed | 04/09/2026 | TamKnm | | | | | | | `GetCommunityCleanupByIdQuery` returns detail with myParticipation. |
| TC_CCU_048 | Xem chi tiết — event không tồn tại. | 1. Login as Citizen.<br>2. Gọi API với eventId không tồn tại. | Error "Không tìm thấy" is displayed. | - Invalid eventId. | Passed | 04/09/2026 | TamKnm | | | | | | | 404 Not Found. |
| TC_CCU_049 | Xem chương trình active của report. | 1. Login as Citizen.<br>2. Mở báo cáo có chương trình active. | Hiển thị thông tin chương trình active (1 per report). | - Report has active community cleanup. | Passed | 04/09/2026 | TamKnm | | | | | | | `GetActiveCommunityCleanupByReportIdQuery`. |
| TC_CCU_050 | Xem chương trình active — report chưa có. | 1. Login as Citizen.<br>2. Mở báo cáo chưa có chương trình. | data = null. Không lỗi (200 OK). | - Report has no active community cleanup. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns null data, not 404. |
| TC_CCU_051 | Xem chương trình đã tham gia (my). | 1. Login as Citizen.<br>2. Navigate to "Chương trình đã tham gia". | Danh sách events user đã join/checkedin/withdrawn. Paginated. | - User has participated. | Passed | 04/09/2026 | TamKnm | | | | | | | `GetMyCommunityCleanupsQuery`. `[Authorize(Roles = "Citizen")]`. |

---

## Authorization & Edge Cases

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_052 | Citizen cố đóng đăng ký. | 1. Login as Citizen.<br>2. Gọi API close-join. | 403 Forbidden. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO,Admin")]`. |
| TC_CCU_053 | Citizen cố hủy chương trình. | 1. Login as Citizen.<br>2. Gọi API cancel. | 403 Forbidden. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO,Admin")]`. |
| TC_CCU_054 | Citizen cố duyệt xác thực. | 1. Login as Citizen.<br>2. Gọi API verify. | 403 Forbidden. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO,Admin")]`. |
| TC_CCU_055 | Citizen cố upload ảnh before. | 1. Login as Citizen.<br>2. Gọi API before-images. | 403 Forbidden. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Cleaner,Admin")]`. |
| TC_CCU_056 | Citizen cố cập nhật tiến độ. | 1. Login as Citizen.<br>2. Gọi API progress. | 403 Forbidden. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Cleaner,Admin")]`. |
| TC_CCU_057 | LEO cố join chương trình (role không phải Citizen). | 1. Login as LEO.<br>2. Gọi API join. | 403 Forbidden. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Citizen")]` on join endpoint. |
| TC_CCU_058 | LEO xem participants — success. | 1. Login as LEO.<br>2. Xem danh sách participants. | Danh sách đầy đủ (LEO được phép). | - LEO is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler allows LEO to view participants. |
| TC_CCU_059 | Không đăng nhập — mọi endpoint cộng đồng. | 1. Gọi bất kỳ endpoint community cleanup mà không có JWT. | 401 Unauthorized. | - User not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | All endpoints require `[Authorize]`. |
| TC_CCU_060 | Check-in — lần thứ 2 (đã check-in rồi). | 1. Login as participant đã CheckedIn.<br>2. Gọi API check-in lần nữa. | Error "Trạng thái không hợp lệ" is displayed. | - Participant already CheckedIn. | Passed | 04/09/2026 | TamKnm | | | | | | | `participant.CheckIn()` throws `InvalidOperationException` for invalid state. |
| TC_CCU_061 | Xem chương trình mở — pagination page 2. | 1. Login as Citizen.<br>2. Scroll xuống load trang 2. | Trang 2 được tải. 20 items/trang default. | - More than 20 open events. | Passed | 04/09/2026 | TamKnm | | | | | | | Pagination `page=2, pageSize=20`. |

---

## State Machine Flow

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_062 | Flow hoàn chỉnh: Create → Join → CheckIn → Start → Progress → Submit → Verify. | 1. LEO tạo chương trình.<br>2. Citizen join.<br>3. Citizen check-in.<br>4. Leader bắt đầu.<br>5. Leader cập nhật 100%.<br>6. Leader nộp xác thực.<br>7. LEO duyệt. | OpenForJoin → JoinClosed (tự hoặc LEO đóng) → InProgress → PendingVerification → Completed. Report → Resolved. | - All actors available. | Passed | 04/09/2026 | TamKnm | | | | | | | Happy path end-to-end. |
| TC_CCU_063 | Flow hủy: Create → Cancel (by LEO). | 1. LEO tạo chương trình.<br>2. Citizen join.<br>3. LEO hủy (reason ≥ 20 chars). | OpenForJoin → Cancelled. Report trở về Verified. Participants Joined → Withdrawn. | - Event has participants. | Passed | 04/09/2026 | TamKnm | | | | | | | Cancel reverts report and withdraws participants. |
| TC_CCU_064 | Flow từ chối: Submit → Reject → tiếp tục dọn → Submit lại → Verify. | 1. Leader nộp xác thực → LEO từ chối (reason).<br>2. Leader dọn thêm → cập nhật progress → nộp lại.<br>3. LEO duyệt. | InProgress → PendingVerification → InProgress (reject) → PendingVerification → Completed. | - All actors available. | Passed | 04/09/2026 | TamKnm | | | | | | | Reject cycle allows retry. |
| TC_CCU_065 | Check-in quá xa không có reason — lý do < 20 ký tự. | 1. Participant check-in > 200m, nhập reason "Xa" (< 20 chars). | Hệ thống chấp nhận reason bất kỳ (không validate length). | - Distance > 200m, short reason. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: Handler chỉ check `string.IsNullOrWhiteSpace(request.Reason)` mà không validate minimum length cho reason. Reason "a" cũng được chấp nhận. Nên thêm check `reason.Length >= 20` để đồng nhất với các rule khác (cancel, reject đều require ≥ 20 chars). |
| TC_CCU_066 | Tạo chương trình — startsAt trong quá khứ. | 1. LEO tạo chương trình với startsAt = ngày hôm qua. | Chương trình được tạo thành công (không validate startsAt > now). | - StartsAt is in the past. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: Validator không check `StartsAt > DateTime.UtcNow`. Cho phép tạo event với thời gian bắt đầu trong quá khứ. Nên thêm rule `RuleFor(x => x.StartsAt).GreaterThan(DateTime.UtcNow)`. |

---

## Map Integration

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_CCU_067 | Map hiển thị badge community cleanup trên pin. | 1. Mở bản đồ.<br>2. Xem pin report có chương trình cộng đồng active. | Pin hiển thị badge/icon "Community Cleanup" kèm eventId. `hasCommunityCleanup = true`. | - Report has active community cleanup. | Passed | 04/09/2026 | TamKnm | | | | | | | Map handler joins CommunityCleanupEvent table. |
| TC_CCU_068 | Map — chương trình Completed không hiện badge. | 1. Mở bản đồ.<br>2. Xem pin report có chương trình Completed. | Pin không hiển thị badge community cleanup. `hasCommunityCleanup = false`. | - Event is Completed. | Passed | 04/09/2026 | TamKnm | | | | | | | Map handler filters `Status != Completed && Status != Cancelled`. |
| TC_CCU_069 | Click badge → navigate tới chi tiết chương trình. | 1. Mở bản đồ.<br>2. Click badge community cleanup trên pin. | App navigate tới màn hình chi tiết chương trình (eventId). | - Pin has community cleanup badge. | Passed | 04/09/2026 | TamKnm | | | | | | | Map response includes eventId for navigation. |
| TC_CCU_070 | Tạo chương trình — report đã có nhưng đã Completed. | 1. LEO mở report đã hoàn thành chương trình trước → tạo mới. | Success. Chương trình mới được tạo (event cũ Completed không block). | - Previous event is Completed. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler only blocks if active event exists (not Completed/Cancelled). |
| TC_CCU_071 | Report bị ẩn — chương trình vẫn hoạt động. | 1. Admin ẩn report.<br>2. Chương trình active vẫn tiếp tục. | Chương trình vẫn hoạt động. Report không hiển thị trên map nhưng event logic tiếp tục. | - Report is hidden. | Passed | 04/09/2026 | TamKnm | | | | | | | IsHidden only affects map visibility, not cleanup logic. |
| TC_CCU_072 | Check-in idempotency. | 1. Participant check-in 2 lần cùng idempotency key. | Lần 2 không gây lỗi. | - Same idempotency key. | Passed | 04/09/2026 | TamKnm | | | | | | | `[SupportsIdempotency]` on check-in endpoint. |

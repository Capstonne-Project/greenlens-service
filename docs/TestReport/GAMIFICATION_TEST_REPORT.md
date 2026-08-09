# Test Report — Gamification

|                      |                                                 |
| -------------------- | ----------------------------------------------- |
| **Feature**          | **Gamification — Điểm, Huy hiệu & Bảng xếp hạng** |
| **Test requirement** |                                                 |
| **Number of TCs**    | **52**                                          |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 50     | 2      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## Xem điểm của tôi (My Points)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_001 | Xem điểm — user có điểm. | 1. Login as Citizen.<br>2. Navigate to "Hồ sơ" → "Điểm thưởng". | Hiển thị: totalPoints, level, isLocked=false, danh sách giao dịch điểm gần đây (reason, points, reportId, createdAt). | - User has gamification record with points. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns `MyPointsResponse` with level calculated from points. |
| TC_GAM_002 | Xem điểm — user mới (chưa có record). | 1. Login as new Citizen (chưa tương tác gì).<br>2. Navigate to "Điểm thưởng". | totalPoints=0, level=1, isLocked=false, transactions rỗng. | - User is new, no UserPoints record. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns defaults when `up is null`: points=0, level=1, locked=false. |
| TC_GAM_003 | Xem điểm — level 2 (≥100 points). | 1. Login as Citizen có 150 điểm.<br>2. Xem điểm. | level = 2. | - User has 150 points. | Passed | 04/09/2026 | TamKnm | | | | | | | Level thresholds: 1=<100, 2=100-499, 3=500-1499, 4=1500-4999, 5=≥5000. |
| TC_GAM_004 | Xem điểm — level 3 (≥500 points). | 1. Login as Citizen có 600 điểm.<br>2. Xem điểm. | level = 3. | - User has 600 points. | Passed | 04/09/2026 | TamKnm | | | | | | | Level 3 threshold: ≥500. |
| TC_GAM_005 | Xem điểm — level 5 (≥5000 points). | 1. Login as Citizen có 5500 điểm.<br>2. Xem điểm. | level = 5 (max). | - User has 5500 points. | Passed | 04/09/2026 | TamKnm | | | | | | | Level 5 threshold: ≥5000. |
| TC_GAM_006 | Xem điểm — pagination giao dịch. | 1. Login as Citizen có nhiều giao dịch.<br>2. Xem trang 2 (page=2, pageSize=10). | Trang 2 hiển thị 10 giao dịch tiếp theo. totalTransactions cho biết tổng số. | - User has >10 transactions. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler paginates transactions: `Skip((page-1)*pageSize).Take(pageSize)`. |
| TC_GAM_007 | Xem điểm — user bị lock gamification. | 1. Login as Citizen bị lock.<br>2. Xem điểm. | isLocked=true, lockedUntil hiển thị ngày hết lock. totalPoints=0 (đã bị trừ). | - User gamification is locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns `IsLocked=true` and `LockedUntil` from UserPoints. |
| TC_GAM_008 | Xem điểm — chưa đăng nhập. | 1. Cố truy cập "Điểm thưởng" mà không đăng nhập. | 401 Unauthorized. Redirect tới login. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize]` on endpoint. |

---

## Xem huy hiệu của tôi (My Badges)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_009 | Xem huy hiệu — user có huy hiệu. | 1. Login as Citizen.<br>2. Navigate to "Hồ sơ" → "Huy hiệu". | Danh sách huy hiệu đã đạt: code, nameVi, nameEn, description, iconUrl, awardedAt. Sorted mới nhất trước. | - User has earned badges. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler joins UserBadge → Badge, ordered by `AwardedAt` descending. |
| TC_GAM_010 | Xem huy hiệu — user chưa có huy hiệu. | 1. Login as new Citizen.<br>2. Navigate to "Huy hiệu". | Danh sách rỗng. Hiển thị message "Chưa có huy hiệu nào." | - User is new, no badges. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns empty list. |
| TC_GAM_011 | Xem huy hiệu — mỗi badge có icon và mô tả. | 1. Login as Citizen có huy hiệu.<br>2. Xem chi tiết 1 huy hiệu. | Badge hiển thị: icon (iconUrl), tên tiếng Việt, tên tiếng Anh, mô tả, ngày đạt được. | - User has badge with icon. | Passed | 04/09/2026 | TamKnm | | | | | | | Badge entity has `IconUrl`, `NameVi`, `NameEn`, `Description`. |

---

## Badge Catalog (Danh mục huy hiệu đầy đủ)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_012 | Xem toàn bộ catalog — đã đạt + chưa đạt. | 1. Login as Citizen.<br>2. Navigate to "Huy hiệu" → "Tất cả huy hiệu". | Tất cả huy hiệu hiển thị: đã mở khóa (isUnlocked=true, awardedAt) và chưa mở khóa (isUnlocked=false, showRequiredPoints/ReportCount/StreakDays). Sorted: unlocked first, then by difficulty. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler loads all active badges + user's earned badges. Shows current progress for locked badges. |
| TC_GAM_013 | Catalog — hiển thị tiến độ (currentProgressValue). | 1. Login as Citizen có 80 điểm (cần 100 cho badge next).<br>2. Xem catalog. | Badge chưa mở khóa hiển thị: requiredPoints=100, currentProgressValue=80. User thấy "Còn 20 điểm nữa". | - User has partial progress. | Passed | 04/09/2026 | TamKnm | | | | | | | `BadgeEligibilityEvaluator.GetCurrentProgressValue` calculates current progress. |
| TC_GAM_014 | Catalog — badge đã đạt không hiển thị progress. | 1. Login as Citizen đã đạt badge.<br>2. Xem catalog. | Badge đã mở khóa: currentProgressValue = null. | - User has earned badge. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns `null` for `CurrentProgressValue` when `isUnlocked = true`. |
| TC_GAM_015 | Catalog — hiển thị badge featured. | 1. Login as Citizen đã set featured badge.<br>2. Xem catalog. | Badge đang featured hiển thị: isFeatured=true. | - User has featured badge set. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `featuredBadgeId == b.Id`. |
| TC_GAM_016 | Catalog — chỉ hiển thị badge active. | 1. Admin deactivate 1 badge.<br>2. User xem catalog. | Badge bị deactivate không hiển thị. | - Some badges inactive. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler loads `badgeRepo.GetAllActiveAsync()`. |

---

## Set Featured Badge (Huy hiệu nổi bật)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_017 | Set featured badge — success. | 1. Login as Citizen.<br>2. Navigate to "Huy hiệu" → chọn 1 badge đã đạt.<br>3. Click "Đặt làm nổi bật". | Success. Badge hiển thị trên hồ sơ công khai. FeaturedBadgeId cập nhật. | - User has earned the badge. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates badge ownership before setting. |
| TC_GAM_018 | Set featured badge — badge chưa mở khóa. | 1. Login as Citizen.<br>2. Cố set featured 1 badge chưa đạt. | Error "Bạn chưa sở hữu huy hiệu này" is displayed. | - User has not earned the badge. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `userBadgeRepo.HasBadgeAsync()` → `Errors.Gamification.BadgeNotOwned`. |
| TC_GAM_019 | Remove featured badge — truyền null. | 1. Login as Citizen.<br>2. Click "Bỏ huy hiệu nổi bật". | Success. FeaturedBadgeId = null. Hồ sơ không hiển thị badge nổi bật. | - User has featured badge. | Passed | 04/09/2026 | TamKnm | | | | | | | Passing `BadgeId = null` clears featured badge. |
| TC_GAM_020 | Set featured badge — thay đổi badge. | 1. Login as Citizen đã có featured badge A.<br>2. Chọn badge B → "Đặt làm nổi bật". | Success. Badge B thay thế A. | - User has multiple badges. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler overwrites previous featured badge. |

---

## Bảng xếp hạng (Leaderboard)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_021 | Leaderboard — AllTime (default). | 1. Mở app (có thể không đăng nhập).<br>2. Navigate to "Bảng xếp hạng". | Top 10 users hiển thị: rank, displayName, avatarUrl, points, level. Period = AllTime. Sorted by totalPoints descending. | - Users have points. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]` endpoint. Default period=AllTime, top=10. |
| TC_GAM_022 | Leaderboard — anonymous access (không cần login). | 1. Mở app không đăng nhập.<br>2. Xem bảng xếp hạng. | Bảng xếp hạng hiển thị bình thường. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[AllowAnonymous]` on leaderboard endpoint. |
| TC_GAM_023 | Leaderboard — Monthly (tháng hiện tại). | 1. Mở bảng xếp hạng.<br>2. Chọn "Tháng này". | Top users theo điểm trong tháng hiện tại. PeriodStart và PeriodEnd hiển thị. | - Users have points in current month. | Passed | 04/09/2026 | TamKnm | | | | | | | Period=Monthly, month/year auto-resolved from current date. |
| TC_GAM_024 | Leaderboard — Monthly (tháng cụ thể). | 1. Mở bảng xếp hạng.<br>2. Chọn period=Monthly, year=2026, month=7. | Top users theo điểm tháng 7/2026. | - Users have points in July 2026. | Passed | 04/09/2026 | TamKnm | | | | | | | Query params `period=Monthly&year=2026&month=7`. |
| TC_GAM_025 | Leaderboard — Yearly. | 1. Mở bảng xếp hạng.<br>2. Chọn period=Yearly, year=2026. | Top users theo điểm năm 2026. | - Users have points in 2026. | Passed | 04/09/2026 | TamKnm | | | | | | | Period=Yearly with year filter. |
| TC_GAM_026 | Leaderboard — Weekly. | 1. Mở bảng xếp hạng.<br>2. Chọn "Tuần này". | Top users theo điểm tuần hiện tại (Mon–Sun). | - Users have points this week. | Passed | 04/09/2026 | TamKnm | | | | | | | Period=Weekly. |
| TC_GAM_027 | Leaderboard — top custom (top=5). | 1. Mở bảng xếp hạng.<br>2. Gọi API với top=5. | Chỉ hiển thị top 5 users. | - Users have points. | Passed | 04/09/2026 | TamKnm | | | | | | | Query param `top=5`. |
| TC_GAM_028 | Leaderboard — top = 0 (invalid). | 1. Gọi API với top=0. | Error "Top must be between 1 and 100." is displayed. | - Invalid top value. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(1, 100)`. |
| TC_GAM_029 | Leaderboard — top > 100 (invalid). | 1. Gọi API với top=200. | Error "Top must be between 1 and 100." is displayed. | - Invalid top value. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(1, 100)`. |
| TC_GAM_030 | Leaderboard — month không hợp lệ (= 13). | 1. Gọi API với period=Monthly, month=13. | Error "Month must be between 1 and 12." is displayed. | - Invalid month. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(1, 12)`. |
| TC_GAM_031 | Leaderboard — month khi period không phải Monthly. | 1. Gọi API với period=Yearly, month=5. | Error "month is only supported when period is Monthly." is displayed. | - Incompatible params. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `Null().When(period != Monthly)`. |
| TC_GAM_032 | Leaderboard — year khi period = AllTime hoặc Weekly. | 1. Gọi API với period=AllTime, year=2026. | Error "year is only supported when period is Monthly or Yearly." is displayed. | - Incompatible params. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `Null().When(period is AllTime or Weekly)`. |
| TC_GAM_033 | Leaderboard — user bị lock không hiển thị. | 1. Admin lock gamification cho User A.<br>2. Xem bảng xếp hạng. | User A không xuất hiện trong leaderboard. | - User A is locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters `!up.IsLocked`. |
| TC_GAM_034 | Leaderboard — user 0 điểm không hiển thị. | 1. Xem bảng xếp hạng. | Users có 0 điểm không xuất hiện. | - Some users have 0 points. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler filters `up.TotalPoints > 0` (AllTime) or `PeriodPoints > 0` (periodic). |
| TC_GAM_035 | Leaderboard — ranking chính xác. | 1. Xem bảng xếp hạng.<br>2. Kiểm tra rank = 1, 2, 3... | Rank = vị trí trong list (1-indexed). User có nhiều điểm nhất rank 1. | - Multiple users with different points. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler uses `Select((e, i) => new { Rank = i + 1, ... })`. |

---

## Admin: Lock Gamification (Fraud Penalty)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_036 | Lock gamification — success. | 1. Login as Admin.<br>2. Navigate to "Quản lý user" → chọn user.<br>3. Click "Khóa gamification".<br>4. Nhập lý do: "Gian lận báo cáo".<br>5. LockDays = 30.<br>6. Xác nhận. | Success. pointsDeducted = totalPoints trước đó. lockedUntil = now + 30 days. User bị trừ hết điểm. | - User has points.<br>- Admin is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `userPoints.Lock(reason, lockDays)` which deducts all points and sets lock. |
| TC_GAM_037 | Lock gamification — user đã bị lock rồi. | 1. Login as Admin.<br>2. Cố lock user đã bị lock. | Error "Gamification đã bị khóa" is displayed. | - User already locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `userPoints.IsLocked` → `Errors.Gamification.AlreadyLocked`. |
| TC_GAM_038 | Lock gamification — user không tồn tại. | 1. Login as Admin.<br>2. Gọi API với userId không tồn tại. | Error "Không tìm thấy user" is displayed. | - Invalid userId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Users.UserNotFound`. |
| TC_GAM_039 | Lock gamification — user chưa có record (0 điểm). | 1. Login as Admin.<br>2. Lock user mới (chưa có UserPoints record). | Success. pointsDeducted = 0. lockedUntil set. UserPoints record được tạo mới. | - User has no gamification record. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler uses `GetOrCreateByUserIdAsync` to create record if needed. |
| TC_GAM_040 | Lock gamification — lockDays custom (= 90). | 1. Login as Admin.<br>2. Lock user với lockDays = 90. | Success. lockedUntil = now + 90 days. | - User has points. | Passed | 04/09/2026 | TamKnm | | | | | | | LockDays is configurable, default 30. |
| TC_GAM_041 | Lock gamification — không phải Admin. | 1. Login as Citizen.<br>2. Cố gọi API lock gamification. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Admin")]` blocks non-admin roles. |
| TC_GAM_042 | Lock gamification — không đăng nhập. | 1. Gọi API lock mà không có JWT. | 401 Unauthorized is returned. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize]` on controller. |

---

## Tự động tích điểm (Event-based)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_043 | Tích điểm — gửi báo cáo được xác minh. | 1. Login as Citizen.<br>2. Gửi báo cáo ô nhiễm.<br>3. LEO xác minh báo cáo.<br>4. Citizen kiểm tra điểm. | totalPoints tăng. Transaction mới: reason=ReportVerified. | - Report verified by LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Event handler `GamificationPointAwarder` listens to ReportVerifiedEvent. |
| TC_GAM_044 | Tích điểm — báo cáo được resolve. | 1. Login as Citizen.<br>2. Báo cáo được team dọn xong (status=Resolved).<br>3. Kiểm tra điểm. | totalPoints tăng. Transaction mới: reason=ReportResolved. | - Report resolved. | Passed | 04/09/2026 | TamKnm | | | | | | | Event handler for ReportResolved awards points. |
| TC_GAM_045 | Tích điểm — user bị lock không nhận điểm. | 1. Admin lock gamification cho User A.<br>2. User A gửi báo cáo → report verified.<br>3. Kiểm tra điểm User A. | totalPoints vẫn = 0. Không có transaction mới. isLocked = true. | - User is locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Point awarder skips locked users. |
| TC_GAM_046 | Tự động cấp huy hiệu — đủ điều kiện. | 1. Login as Citizen.<br>2. Đạt đủ điều kiện badge (vd. 100 điểm).<br>3. Kiểm tra "Huy hiệu". | Badge mới xuất hiện trong danh sách. Notification "Chúc mừng! Bạn đã nhận huy hiệu..." | - User reaches badge threshold. | Passed | 04/09/2026 | TamKnm | | | | | | | `BadgeEligibilityEvaluator` + `CheckBadges` handler auto-checks after point award. |
| TC_GAM_047 | Huy hiệu streak — gửi báo cáo liên tục N ngày. | 1. Login as Citizen.<br>2. Gửi ít nhất 1 report mỗi ngày liên tục 7 ngày.<br>3. Kiểm tra huy hiệu. | Badge streak (vd. "7 ngày liên tục") được cấp tự động. | - User has 7-day streak. | Passed | 04/09/2026 | TamKnm | | | | | | | `ReportStreakCalculator` calculates consecutive reporting days. |

---

## Edge Cases & Security

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_GAM_048 | User không thể xem điểm user khác. | 1. Login as Citizen A.<br>2. Gọi API my-points. | Chỉ xem được điểm của chính mình (userId lấy từ JWT token). | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller extracts userId from `ClaimTypes.NameIdentifier`. |
| TC_GAM_049 | User không thể set featured badge cho user khác. | 1. Login as Citizen A.<br>2. Gọi API set featured badge. | userId tự lấy từ JWT. Không thể truyền userId của người khác. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller extracts userId from JWT, not from request body. |
| TC_GAM_050 | Leaderboard hiển thị displayName, không hiển thị email/phone. | 1. Xem bảng xếp hạng. | Chỉ hiển thị: displayName (FullName), avatarUrl. KHÔNG hiển thị email, phone. | - Users on leaderboard. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler only projects `FullName` and `AvatarUrl` from User entity. |
| TC_GAM_051 | Controller dùng Guid.Parse trực tiếp từ JWT claims. | 1. Login as any user.<br>2. Gọi GET /v1/gamification/my-points. | Thành công nếu JWT hợp lệ. | - User is logged in. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG (Security): Controller dùng `Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)` với null-forgiving operator. Nếu claim bị missing (vd. JWT tampered), sẽ ném `NullReferenceException` hoặc `FormatException` → trả 500 thay vì 401. Nên dùng `TryParse` + trả `Unauthorized` rõ ràng, hoặc dùng `ICurrentUser` interface thay vì parse trực tiếp. |
| TC_GAM_052 | Lock gamification — reason rỗng. | 1. Login as Admin.<br>2. Lock user với reason trống. | Thành công (không có validator cho reason). | - Admin is logged in. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: `LockGamificationCommand` không có FluentValidation validator. Reason rỗng được chấp nhận, dẫn đến record lock mà không có lý do. Cần thêm validator require Reason NotEmpty và MinLength (vd. ≥10 ký tự). |

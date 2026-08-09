# Test Report — Users

|                      |                              |
| -------------------- | ---------------------------- |
| **Feature**          | **Users**                    |
| **Test requirement** |                              |
| **Number of TCs**    | **48**                       |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 47     | 1      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## View My Profile

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_001 | View my profile — success. | 1. Login as any user.<br>2. Navigate to "Hồ sơ" page. | Profile page displays: email, full name, phone number, avatar, role, email verified status, achievements, total points, level, rank, and featured badge. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns UserProfileDto with gamification data (points, level, rank, badges). |
| TC_USR_002 | View my profile — user not found. | 1. Login with a valid token for a user that has been deleted from the database.<br>2. Navigate to "Hồ sơ" page. | Error message "Không tìm thấy người dùng" is displayed. | - Token is valid but user record is deleted (hard delete). | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Users.UserNotFound`. |
| TC_USR_003 | View my profile — not logged in. | 1. Open "Hồ sơ" page without logging in. | User is redirected to Login page. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` on class level. |
| TC_USR_004 | View my profile — gamification locked account. | 1. Login as user whose gamification is locked (suspected fraud).<br>2. Navigate to "Hồ sơ" page. | Profile displays with `isGamificationLocked = true`. Rank is null. Points still visible to self. | - User's gamification is locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `gamification?.IsLocked ?? false` and skips rank computation when locked. |
| TC_USR_005 | View my profile — featured badge displayed. | 1. Login as user who has set a featured badge.<br>2. Navigate to "Hồ sơ" page. | Featured badge is displayed with icon, name (Vi + En). | - User has set FeaturedBadgeId. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler loads badge by `user.FeaturedBadgeId` and returns FeaturedBadgeDto. |

---

## Update My Profile

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_006 | Update full name — success. | 1. Login as any user.<br>2. Navigate to "Chỉnh sửa hồ sơ".<br>3. Change "Họ và tên" to "Nguyễn Văn Tâm".<br>4. Click "Lưu". | Success message "Cập nhật hồ sơ thành công." is displayed. Name is updated. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `user.UpdateProfile(request.FullName)` and saves. |
| TC_USR_007 | Update full name — too long (> 200 chars). | 1. Login as any user.<br>2. Navigate to "Chỉnh sửa hồ sơ".<br>3. Enter a name with more than 200 characters.<br>4. Click "Lưu". | Error message "Full name must not exceed 200 characters." is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(200)` for FullName. |
| TC_USR_008 | Update profile — send null full name (no change). | 1. Login as any user.<br>2. Navigate to "Chỉnh sửa hồ sơ".<br>3. Click "Lưu" without changing name. | Profile is updated successfully (no-op for null fields). | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator skips validation when FullName is null (`.When(x => x.FullName is not null)`). |
| TC_USR_009 | Update profile — user not found. | 1. Login with a token for a deleted user.<br>2. Attempt to update profile. | Error message "Không tìm thấy người dùng" is displayed. | - Token valid, user record deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Users.UserNotFound`. |
| TC_USR_010 | Update profile — not logged in. | 1. Attempt to call update profile API without authentication. | 401 Unauthorized is returned. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` on class level. |

---

## Upload Avatar

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_011 | Upload avatar — valid JPEG image (< 5MB). | 1. Login as any user.<br>2. Navigate to "Chỉnh sửa hồ sơ".<br>3. Click "Đổi ảnh đại diện".<br>4. Select a JPEG image (< 5MB).<br>5. Confirm. | Avatar is updated. New avatar URL is displayed. Success message "Cập nhật ảnh đại diện thành công." is shown. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler uploads to R2 Cloudflare, updates `user.AvatarUrl`. |
| TC_USR_012 | Upload avatar — valid PNG image. | 1. Login as any user.<br>2. Click "Đổi ảnh đại diện".<br>3. Select a PNG image (< 5MB).<br>4. Confirm. | Avatar is updated. New avatar URL is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `AllowedContentTypes` includes "image/png". |
| TC_USR_013 | Upload avatar — valid WebP image. | 1. Login as any user.<br>2. Click "Đổi ảnh đại diện".<br>3. Select a WebP image (< 5MB).<br>4. Confirm. | Avatar is updated. New avatar URL is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `AllowedContentTypes` includes "image/webp". |
| TC_USR_014 | Upload avatar — no file selected. | 1. Login as any user.<br>2. Click "Đổi ảnh đại diện".<br>3. Submit without selecting any file. | Error message "Vui lòng chọn file ảnh." is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller checks `file is null || file.Length == 0` → 400. |
| TC_USR_015 | Upload avatar — file exceeds 5MB. | 1. Login as any user.<br>2. Click "Đổi ảnh đại diện".<br>3. Select an image larger than 5MB.<br>4. Confirm. | Error message indicating file too large is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `request.FileSize > MaxFileSizeBytes (5MB)` → `Errors.Users.FileTooLarge`. |
| TC_USR_016 | Upload avatar — invalid MIME type (PDF). | 1. Login as any user.<br>2. Click "Đổi ảnh đại diện".<br>3. Select a PDF file.<br>4. Confirm. | Error message indicating invalid file type is displayed. Accepted: JPEG, PNG, WebP. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `!AllowedContentTypes.Contains(request.ContentType)` → `Errors.Users.InvalidFileType`. |
| TC_USR_017 | Upload avatar — R2 storage upload fails. | 1. Login as any user.<br>2. Select a valid image file.<br>3. Attempt to upload when R2 storage is unreachable. | Error message "Upload ảnh đại diện thất bại" is displayed. | - User is logged in.<br>- R2 Cloudflare is down. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches `Exception` from `fileStorage.UploadAsync` → `Errors.Users.StorageUploadFailed`. |
| TC_USR_018 | Upload avatar — user not found. | 1. Login with a token for a deleted user.<br>2. Attempt to upload avatar. | Error message "Không tìm thấy người dùng" is displayed. | - Token valid, user record deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Users.UserNotFound`. |

---

## View Public User Profile

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_019 | View public profile of another user — success. | 1. Login as any user.<br>2. Click on a reporter's name or avatar on a report card. | Public profile page displays: display name, avatar, role, total points, level, rank, report count, achievements, featured badge, and join date. No email/phone shown. | - User is logged in.<br>- Target user exists. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler does NOT return email, phone, GoogleId. Returns display name via `CommentAccess.ResolveAuthorDisplayName`. |
| TC_USR_020 | View public profile — user not found. | 1. Login as any user.<br>2. Navigate to a public profile with non-existent user ID. | Error message "Không tìm thấy người dùng" or 404 is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Users.UserNotFound`. |
| TC_USR_021 | View public profile — banned user returns 404. | 1. Login as any user.<br>2. Navigate to public profile of a banned user. | 404 "Không tìm thấy" is displayed. Banned user profile is hidden from public. | - Target user is banned. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user.IsBanned` → `Errors.Users.UserNotFound` (same as not found). |
| TC_USR_022 | View public profile — no email/phone exposed. | 1. Login as any user.<br>2. View another user's public profile. | No email, phone number, or Google ID is visible in the response. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `PublicUserProfileDto` does not contain Email, PhoneNumber, GoogleId fields. |
| TC_USR_023 | View public profile — gamification locked user. | 1. Login as any user.<br>2. View public profile of a user whose gamification is locked. | Profile shows null for totalPoints, level, rank. No achievements or featured badge displayed. | - Target user's gamification is locked. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `stats is { IsLocked: true }` → sets pointsVisible=false, returns null for all gamification. |
| TC_USR_024 | View public profile — CompanyStaff/Cleaner shows generic display name. | 1. Login as any user.<br>2. View public profile of a CompanyStaff or Cleaner user. | Display name shows a generic label instead of real name. | - Target user has role CompanyStaff or Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler uses `CommentAccess.ResolveAuthorDisplayName(role, fullName)` which returns generic label for staff. |
| TC_USR_025 | View public reports of another user. | 1. Login as any user.<br>2. Click on "Báo cáo" tab on a user's public profile. | Grid of user's public reports is displayed. Anonymous reports and hidden reports are excluded. | - User is logged in.<br>- Target user has submitted reports. | Passed | 04/09/2026 | TamKnm | | | | | | | `GetUserPublicReportsQuery` filters `!r.IsHidden && !r.HideReporterName`. |
| TC_USR_026 | View public reports — user not found. | 1. Login as any user.<br>2. Access public reports for a non-existent user ID. | 404 error is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Query handles user not found scenario. |

---

## Verify Phone (Firebase Phone Auth)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_027 | Verify phone via Firebase — success. | 1. Login as any user.<br>2. Navigate to "Xác thực số điện thoại".<br>3. Enter phone number and receive OTP via Firebase SDK.<br>4. Enter OTP on the app.<br>5. Submit Firebase ID token to backend. | Success message "Xác thực số điện thoại thành công." is displayed. Phone is updated in profile. | - User is logged in.<br>- Firebase Phone Auth configured.<br>- Phone not used by another account. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler verifies token, normalizes phone, updates `user.VerifyPhone(phone)`. |
| TC_USR_028 | Verify phone — empty Firebase token. | 1. Login as any user.<br>2. Submit empty Firebase ID token. | Error message "Firebase ID token không được để trống." is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` on `FirebaseIdToken`. |
| TC_USR_029 | Verify phone — invalid Firebase token. | 1. Login as any user.<br>2. Submit an invalid or expired Firebase ID token. | Error message indicating invalid Firebase token is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `phoneInfo is null` → `Errors.Phone.FirebaseTokenInvalid`. |
| TC_USR_030 | Verify phone — phone number missing from token. | 1. Login as any user.<br>2. Submit a valid Firebase token that does not contain a phone number. | Error message indicating phone missing is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `string.IsNullOrWhiteSpace(phone)` → `Errors.Phone.FirebasePhoneMissing`. |
| TC_USR_031 | Verify phone — phone already used by another account. | 1. Login as Citizen A.<br>2. Verify a phone number that is already registered to Citizen B. | Error message "Số điện thoại đã được sử dụng bởi tài khoản khác." is displayed. | - User is logged in.<br>- Phone belongs to another user. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `users.PhoneExistsIncludingDeletedAsync` → `Errors.Phone.PhoneAlreadyUsed`. |
| TC_USR_032 | Verify phone — phone normalization (0xxx → 84xxx). | 1. Login as any user.<br>2. Verify phone number starting with "0" (e.g. "0912345678"). | Phone is saved as "84912345678" (normalized to international format). | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `NormalizePhone` converts "0xxx" → "84xxx", "+84xxx" → "84xxx". |
| TC_USR_033 | Verify phone — user not found. | 1. Login with token for a deleted user.<br>2. Attempt to verify phone. | Error message "Không tìm thấy người dùng" is displayed. | - Token valid, user deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Auth.UserNotFound`. |

---

## Accept Data Consent

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_034 | Accept data consent — first time. | 1. Login as any new user.<br>2. On first app launch, a consent dialog appears.<br>3. Click "Đồng ý". | Success message "Đã chấp nhận chính sách xử lý dữ liệu." is displayed. `HasDataConsent` is set to true. | - User is logged in.<br>- User has not accepted consent yet. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `user.AcceptDataConsent()` and saves. |
| TC_USR_035 | Accept data consent — idempotent (already accepted). | 1. Login as a user who has already accepted consent.<br>2. Call accept consent API again. | No error. Returns success silently (idempotent). | - User already accepted consent. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user.HasDataConsent` → returns `Result.Success()` without saving. |
| TC_USR_036 | Accept data consent — user not found. | 1. Login with token for a deleted user.<br>2. Call accept consent API. | Error message "Không tìm thấy người dùng" is displayed. | - Token valid, user deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Auth.UserNotFound`. |
| TC_USR_037 | Accept data consent — not logged in. | 1. Call accept consent API without authentication. | 401 Unauthorized is returned. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` on class level. |

---

## Export Personal Data

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_038 | Export personal data — JSON format. | 1. Login as any user.<br>2. Navigate to "Cài đặt" → "Quyền riêng tư".<br>3. Click "Tải xuống dữ liệu cá nhân".<br>4. Select format "JSON".<br>5. Click "Tải xuống". | JSON file is downloaded containing: profile, reports, notifications (max 500), gamification (points + badges). File name: `my_data_yyyyMMdd_HHmmss.json`. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler gathers all personal data, serializes to JSON with camelCase naming and indented formatting. |
| TC_USR_039 | Export personal data — CSV format. | 1. Login as any user.<br>2. Navigate to "Cài đặt" → "Quyền riêng tư".<br>3. Click "Tải xuống dữ liệu cá nhân".<br>4. Select format "CSV".<br>5. Click "Tải xuống". | CSV file is downloaded with sections: PROFILE, REPORTS, NOTIFICATIONS, GAMIFICATION. File name: `my_data_yyyyMMdd_HHmmss.csv`. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `GenerateCsv(exportData)` with UTF-8 BOM. |
| TC_USR_040 | Export personal data — user with no reports. | 1. Login as a new user with no reports.<br>2. Export data as JSON. | JSON file is downloaded with empty Reports array. | - User is logged in.<br>- User has no reports. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns empty lists for reports, notifications, badges. |
| TC_USR_041 | Export personal data — notifications capped at 500. | 1. Login as a user with 1000+ notifications.<br>2. Export data as JSON. | JSON file contains at most 500 notifications (most recent). | - User has many notifications. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler uses `.Take(500)` cap on notifications query. |
| TC_USR_042 | Export personal data — user not found. | 1. Login with token for a deleted user.<br>2. Attempt to export data. | Error message "Không tìm thấy người dùng" is displayed. | - Token valid, user deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null` → `Errors.Auth.UserNotFound`. |
| TC_USR_043 | Export personal data — not logged in. | 1. Call export API without authentication. | 401 Unauthorized is returned. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` on class level. |

---

## Authorization & Edge Cases

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_USR_044 | All user endpoints require authentication. | 1. Attempt to call GET /v1/users/profile, PUT /v1/users/profile, POST /v1/users/avatar, POST /v1/users/phone/verify-firebase, POST /v1/users/me/consent, GET /v1/users/me/data-export without JWT token. | All return 401 Unauthorized. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller has `[Authorize]` on class level — applies to all endpoints. |
| TC_USR_045 | View profile — Citizen, LEO, Admin all can access. | 1. Login as Citizen, LEO, and Admin in separate sessions.<br>2. Each navigates to "Hồ sơ" page. | All three roles can view their own profiles. | - Users of each role exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` (no role restriction) — all authenticated users can access. |
| TC_USR_046 | Update profile — full name exactly 200 chars (boundary). | 1. Login as any user.<br>2. Enter full name with exactly 200 characters.<br>3. Click "Lưu". | Profile is updated successfully. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(200)` — exactly 200 passes. |
| TC_USR_047 | Upload avatar — file exactly 5MB (boundary). | 1. Login as any user.<br>2. Select an image exactly 5MB in size.<br>3. Upload. | Avatar is updated successfully (5MB is at the limit). | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `request.FileSize > MaxFileSizeBytes` — exactly 5MB (5*1024*1024 bytes) passes. |
| TC_USR_048 | Upload avatar — invalid MIME type (GIF). | 1. Login as any user.<br>2. Select a GIF image file.<br>3. Upload. | Error message indicating invalid file type is displayed. Only JPEG, PNG, WebP are accepted. | - User is logged in. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: AllowedContentTypes does NOT include "image/gif". While this is intentional behavior (GIF not supported), the user-facing error message should explicitly list accepted types (JPEG, PNG, WebP). However, the handler returns generic `Errors.Users.InvalidFileType` without specifying which types are allowed. This may confuse users. |

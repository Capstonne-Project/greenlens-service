# Test Report — Pollution Reports

|                      |                              |
| -------------------- | ---------------------------- |
| **Feature**          | **Pollution Reports**        |
| **Test requirement** |                              |
| **Number of TCs**    | **196**                      |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 0      | 0      | 196     | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## AI Analyze Image (Step 1)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_001 | Analyze image successfully with valid photo. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Click "Chọn ảnh" and select a valid JPEG photo (< 20MB) of a pollution site.<br>4. Wait for AI analysis to complete. | AI result is displayed: classification, confidence, severity suggestion, and suggested category auto-filled. "Tiếp tục" button is enabled. | - User is logged in as Citizen.<br>- Camera/gallery permission granted. | | | | | | | | | | |
| TC_RPT_002 | Analyze image — no file selected. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Click "Phân tích" without selecting any image. | Error message "Vui lòng chọn file ảnh." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_003 | Analyze image — file exceeds 20MB. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Select an image file larger than 20MB.<br>4. Attempt to upload. | Error message "File ảnh vượt quá 20MB." is displayed. Upload is blocked. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_004 | Analyze image — AI classifies as irrelevant/abusive. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Upload an image that is not pollution-related (e.g. selfie, food).<br>4. Wait for AI analysis. | Warning message is displayed indicating the image is not valid for reporting. "Tiếp tục" button is disabled. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_005 | Analyze image — AI service is unavailable. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Upload a valid image when AI service is down. | Error message "AI Service tạm thời không khả dụng" is displayed. User can retry later. | - User is logged in as Citizen.<br>- AI service is offline. | | | | | | | | | | |
| TC_RPT_006 | Analyze uploaded image (presign flow) — valid. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Upload image via presign URL flow to R2.<br>4. Call analyze-uploaded with correct key and metadata. | AI analysis result is returned with tempImageId (TTL 15 min) and suggested category. | - User is logged in as Citizen.<br>- Image already uploaded to R2. | | | | | | | | | | |
| TC_RPT_007 | Analyze uploaded image — R2 object not found. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Provide a non-existent R2 key for analysis. | Error message "Object R2 không tồn tại" is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_008 | Analyze image — user is not logged in. | 1. Open "Tạo báo cáo" page without logging in.<br>2. Attempt to upload an image. | User is redirected to Login page. | - User is not logged in. | | | | | | | | | | |

---

## Submit Pollution Report

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_009 | Submit report successfully with AI flow (TempImageId). | 1. Login as Citizen.<br>2. Complete AI analysis (Step 1) with a valid image.<br>3. Fill category, severity, description (≥ 10 chars), GPS location within Vietnam.<br>4. Enter address and ward/province codes.<br>5. Click "Gửi báo cáo". | Success message "Đã gửi báo cáo thành công." is displayed. Report appears in "Báo cáo của tôi" with status "Submitted". OTP/notification email may be sent. | - User is logged in as Citizen.<br>- AI analysis completed.<br>- Data consent accepted. | | | | | | | | | | |
| TC_RPT_010 | Submit report successfully with manual flow (Images). | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Upload 1–5 images via presign URL.<br>4. Fill category, severity, description, GPS coordinates.<br>5. Click "Gửi báo cáo". | Success message is displayed. Report created with status "Submitted". Images are persisted. | - User is logged in as Citizen.<br>- Images uploaded to R2. | | | | | | | | | | |
| TC_RPT_011 | Submit report — missing image (no TempImageId, no Images). | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Fill all fields except do not upload any image.<br>4. Click "Gửi báo cáo". | Error message "Phải cung cấp Images hoặc TempImageId." is displayed. Form submission is blocked. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_012 | Submit report — missing category. | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Leave "Loại ô nhiễm" field empty.<br>4. Fill other required fields.<br>5. Click "Gửi báo cáo". | Error message "CategoryId is required." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_013 | Submit report — invalid severity value. | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Set severity to an invalid value.<br>4. Click "Gửi báo cáo". | Error message "Severity is invalid." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_014 | Submit report — description too short (< 10 chars). | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Enter description "abc" (less than 10 characters).<br>4. Fill other fields.<br>5. Click "Gửi báo cáo". | Error message "Mô tả phải từ 10–1000 ký tự." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_015 | Submit report — description too long (> 1000 chars). | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Enter a description with more than 1000 characters.<br>4. Click "Gửi báo cáo". | Error message "Description must not exceed 1000 characters." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_016 | Submit report — latitude out of Vietnam bounds (BR-REP-003). | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Enter latitude = 5.0 (below 8.0).<br>4. Fill other fields.<br>5. Click "Gửi báo cáo". | Error message "Latitude must be between 8 and 24." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_017 | Submit report — longitude out of Vietnam bounds (BR-REP-003). | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Enter longitude = 115.0 (above 110.0).<br>4. Fill other fields.<br>5. Click "Gửi báo cáo". | Error message "Longitude must be between 102 and 110." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_018 | Submit report — profanity in description (BR-REP-004). | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Enter a description containing profanity/inappropriate words.<br>4. Click "Gửi báo cáo". | Error message indicating inappropriate description is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_019 | Submit report — category does not exist. | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Select a category that has been deactivated or does not exist.<br>4. Click "Gửi báo cáo". | Error message indicating category not found is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_020 | Submit report — rate limit exceeded (BR-REP-010: 5/h). | 1. Login as Citizen.<br>2. Submit 5 reports within 1 hour successfully.<br>3. Attempt to submit a 6th report. | Error message "Bạn đã gửi quá nhiều báo cáo. Vui lòng thử lại sau X phút." is displayed. | - User is logged in as Citizen.<br>- 5 reports already submitted within the last hour. | | | | | | | | | | |
| TC_RPT_021 | Submit report — exceed max 5 images per report (BR-REP-002). | 1. Login as Citizen.<br>2. Upload 6 images for the report.<br>3. Fill other fields.<br>4. Click "Gửi báo cáo". | Error message "Tối đa 5 ảnh mỗi báo cáo." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_022 | Submit report — image URL is not HTTPS. | 1. Login as Citizen.<br>2. Provide an image URL starting with "http://" instead of "https://".<br>3. Click "Gửi báo cáo". | Error message "URL ảnh phải là https tuyệt đối hợp lệ." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_023 | Submit report — invalid MIME type for image. | 1. Login as Citizen.<br>2. Upload a file with MIME type "application/pdf".<br>3. Click "Gửi báo cáo". | Error message indicating invalid MIME type is displayed. Accepted types: image/jpeg, image/png, image/webp, image/heic, image/heif. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_024 | Submit report — WardCode without ProvinceCode. | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Enter WardCode but leave ProvinceCode empty.<br>4. Click "Gửi báo cáo". | Error message "ProvinceCode and WardCode must both be set together or both omitted." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_025 | Submit report — invalid ward/province pair. | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Enter a WardCode that does not belong to the given ProvinceCode.<br>4. Click "Gửi báo cáo". | Error message indicating invalid ward/province pair is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_026 | Submit report — user has not accepted data consent (BR-DAT-005). | 1. Login as Citizen who has not accepted data consent.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Upload a valid image and fill all fields.<br>4. Click "Gửi báo cáo". | Error message "Bạn cần đồng ý chính sách dữ liệu trước khi gửi báo cáo." is displayed. | - User is logged in as Citizen.<br>- Data consent not accepted. | | | | | | | | | | |
| TC_RPT_027 | Submit report — TempImageId expired (> 15 min TTL). | 1. Login as Citizen.<br>2. Complete AI analysis.<br>3. Wait more than 15 minutes.<br>4. Click "Gửi báo cáo". | Error message indicating temp image not found is displayed. User needs to re-analyze. | - User is logged in as Citizen.<br>- TempImageId has expired. | | | | | | | | | | |
| TC_RPT_028 | Submit report — duplicate detection flags report (BR-REP-030). | 1. Login as Citizen.<br>2. Submit a valid report at a location where an existing Verified report with same category exists within 25m.<br>3. Observe the report detail. | Report is created successfully. Report detail shows "Nghi ngờ trùng lặp" flag with reference to the existing report. | - User is logged in as Citizen.<br>- A Verified report with same category exists nearby. | | | | | | | | | | |
| TC_RPT_029 | Submit report — storage URL is not owned by system. | 1. Login as Citizen.<br>2. Provide an image URL that does not belong to the system's CDN/R2.<br>3. Click "Gửi báo cáo". | Error message indicating invalid storage URL is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_030 | Submit report — user is not logged in. | 1. Open "Tạo báo cáo" page without logging in.<br>2. Attempt to submit a report. | User is redirected to Login page. | - User is not logged in. | | | | | | | | | | |
| TC_RPT_031 | Submit report with optional waste tags. | 1. Login as Citizen.<br>2. Upload a valid image.<br>3. Fill all required fields.<br>4. Select 2 waste tags (e.g. "Rác sinh hoạt", "Rác y tế").<br>5. Click "Gửi báo cáo". | Report is created successfully with selected waste tags attached. | - User is logged in as Citizen.<br>- Waste tags exist and are active. | | | | | | | | | | |
| TC_RPT_032 | Submit report — waste tag does not exist. | 1. Login as Citizen.<br>2. Upload a valid image and fill required fields.<br>3. Select a waste tag that has been removed from the system.<br>4. Click "Gửi báo cáo". | Error message indicating waste tag not found is displayed. | - User is logged in as Citizen. | | | | | | | | | | |

---

## View Reports

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_033 | View report list — default page. | 1. Login as any authenticated user.<br>2. Navigate to "Báo cáo" page. | Report list is displayed with pagination (default page 1, 20 items). Each card shows code, category, status, severity, and thumbnail. | - User is logged in. | | | | | | | | | | |
| TC_RPT_034 | View report list — filter by status. | 1. Login as any authenticated user.<br>2. Navigate to "Báo cáo" page.<br>3. Select status filter "Đã xác minh" (Verified). | Only reports with status "Verified" are shown in the list. | - User is logged in.<br>- Reports with various statuses exist. | | | | | | | | | | |
| TC_RPT_035 | View report list — filter by category. | 1. Login as any authenticated user.<br>2. Navigate to "Báo cáo" page.<br>3. Select a specific pollution category from the dropdown. | Only reports matching the selected category are shown. | - User is logged in. | | | | | | | | | | |
| TC_RPT_036 | View report list — search by keyword. | 1. Login as any authenticated user.<br>2. Navigate to "Báo cáo" page.<br>3. Type a keyword (e.g. report code or address fragment) into the search box. | Reports matching the keyword in code, description, or address are shown. | - User is logged in. | | | | | | | | | | |
| TC_RPT_037 | View report detail — valid report ID. | 1. Login as any authenticated user.<br>2. Click on a report card from the list. | Report detail page is displayed with full information: images, description, location, category, severity, status history, and media. | - User is logged in.<br>- Report exists. | | | | | | | | | | |
| TC_RPT_038 | View report detail — report not found. | 1. Login as any authenticated user.<br>2. Navigate to report detail with a non-existent report ID. | Error message "Không tìm thấy" is displayed or 404 page is shown. | - User is logged in. | | | | | | | | | | |
| TC_RPT_039 | View "Báo cáo của tôi" — only own reports shown. | 1. Login as Citizen.<br>2. Navigate to "Báo cáo của tôi" page. | Only reports created by the logged-in user are displayed. Reports from other users are not visible. | - User is logged in as Citizen.<br>- User has submitted reports. | | | | | | | | | | |
| TC_RPT_040 | View "Báo cáo của tôi" — filter by status. | 1. Login as Citizen.<br>2. Navigate to "Báo cáo của tôi" page.<br>3. Select status filter "InProgress". | Only user's reports with status "InProgress" are shown. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_041 | View report status history. | 1. Login as any authenticated user.<br>2. Open a report detail page.<br>3. Click "Lịch sử trạng thái" tab. | Timeline of status changes is shown with timestamp and the person who made each change. | - User is logged in.<br>- Report has multiple status changes. | | | | | | | | | | |

---

## Delete Report (Citizen)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_042 | Delete own report in Submitted status — success (BR-REP-017). | 1. Login as Citizen.<br>2. Navigate to "Báo cáo của tôi".<br>3. Open a report with status "Submitted".<br>4. Click "Xóa báo cáo".<br>5. Confirm deletion in the popup dialog. | Success message "Đã xóa báo cáo." is displayed. Report disappears from the list. | - User is logged in as Citizen.<br>- Report is in Submitted status.<br>- No AI or Officer verification has occurred. | | | | | | | | | | |
| TC_RPT_043 | Delete report — report is already verified (cannot delete). | 1. Login as Citizen.<br>2. Open a report that has been Verified by LEO.<br>3. Click "Xóa báo cáo". | Error message "Báo cáo đã được xác nhận, không thể xóa." is displayed. Delete button may be hidden. | - User is logged in as Citizen.<br>- Report status is Verified. | | | | | | | | | | |
| TC_RPT_044 | Delete report — not the report owner. | 1. Login as Citizen A.<br>2. Navigate to detail of a report created by Citizen B.<br>3. Attempt to delete it. | Error message "Không phải người tạo báo cáo" is displayed. Delete action is not available. | - User is logged in as Citizen A.<br>- Report belongs to Citizen B. | | | | | | | | | | |
| TC_RPT_045 | Delete report — report already deleted. | 1. Login as Citizen.<br>2. Attempt to delete a report that was already soft-deleted. | Error message indicating report already deleted is displayed. | - User is logged in as Citizen.<br>- Report has been soft-deleted. | | | | | | | | | | |
| TC_RPT_046 | Delete report — report not found. | 1. Login as Citizen.<br>2. Attempt to delete a report with a non-existent ID. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as Citizen. | | | | | | | | | | |

---

## LEO — Verify Report

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_047 | Verify report successfully (Submitted → Verified). | 1. Login as LEO.<br>2. Navigate to "Hàng đợi báo cáo".<br>3. Open a report with status "Submitted" in LEO's ward.<br>4. Review the report details and images.<br>5. Click "Xác minh". | Success message "Đã xác minh báo cáo thành công." is displayed. Report status changes to "Verified". | - User is logged in as LEO.<br>- Report is Submitted and in LEO's ward. | | | | | | | | | | |
| TC_RPT_048 | Verify report with severity override. | 1. Login as LEO.<br>2. Open a Submitted report.<br>3. Change severity from "Low" to "High".<br>4. Click "Xác minh". | Report is verified with the overridden severity "High". | - User is logged in as LEO.<br>- Report is Submitted. | | | | | | | | | | |
| TC_RPT_049 | Verify report with category override. | 1. Login as LEO.<br>2. Open a Submitted report.<br>3. Change the category to a different valid category.<br>4. Click "Xác minh". | Report is verified with the overridden category. | - User is logged in as LEO.<br>- Both categories exist and are active. | | | | | | | | | | |
| TC_RPT_050 | Verify report — override category does not exist. | 1. Login as LEO.<br>2. Open a Submitted report.<br>3. Select an invalid/deactivated category for override.<br>4. Click "Xác minh". | Error message indicating category not found is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_051 | Verify report — report not in Submitted status. | 1. Login as LEO.<br>2. Open a report that is already Verified.<br>3. Click "Xác minh". | Error message "Trạng thái không hợp lệ" is displayed. | - User is logged in as LEO.<br>- Report is not Submitted. | | | | | | | | | | |
| TC_RPT_052 | Verify report — conflict of interest (BR-OFF-004). | 1. Login as LEO who is also the reporter of the report.<br>2. Open the report in the queue.<br>3. Click "Xác minh". | Error message "Xung đột lợi ích — không thể xác minh báo cáo của chính mình." is displayed. | - LEO is logged in.<br>- LEO created this report as Citizen. | | | | | | | | | | |
| TC_RPT_053 | Verify report — outside jurisdiction (BR-ORG-012). | 1. Login as LEO assigned to Ward A.<br>2. Open a report that is assigned to Ward B's office.<br>3. Click "Xác minh". | Error message indicating outside jurisdiction is displayed. | - LEO is assigned to a different office than the report. | | | | | | | | | | |
| TC_RPT_054 | Verify report — report not found. | 1. Login as LEO.<br>2. Attempt to verify a report with a non-existent ID. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_055 | Verify report with waste tags. | 1. Login as LEO.<br>2. Open a Submitted report.<br>3. Select waste tags during verification.<br>4. Click "Xác minh". | Report is verified with the selected waste tags attached. | - User is logged in as LEO.<br>- Waste tags exist and are active. | | | | | | | | | | |

---

## LEO — Reject Report

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_056 | Reject report successfully with valid reason (≥ 20 chars). | 1. Login as LEO.<br>2. Open a report with status "Submitted".<br>3. Click "Từ chối".<br>4. Enter reason with at least 20 characters (e.g. "Ảnh không rõ ràng, không thấy hiện trạng ô nhiễm").<br>5. Confirm rejection. | Success message "Đã từ chối báo cáo." is displayed. Report status changes to "Rejected". | - User is logged in as LEO.<br>- Report is Submitted. | | | | | | | | | | |
| TC_RPT_057 | Reject report — reason too short (< 20 chars, BR-REP-022). | 1. Login as LEO.<br>2. Open a Submitted report.<br>3. Click "Từ chối".<br>4. Enter reason "Không hợp lệ" (< 20 chars).<br>5. Confirm rejection. | Error message "Lý do quá ngắn" is displayed. Rejection is blocked. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_058 | Reject report — report not in Submitted status. | 1. Login as LEO.<br>2. Open a report that is already Verified or InProgress.<br>3. Click "Từ chối". | Error message "Trạng thái không hợp lệ" is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_059 | Reject report — conflict of interest (BR-OFF-004). | 1. Login as LEO who created the report.<br>2. Open the report.<br>3. Click "Từ chối" with valid reason. | Error message indicating conflict of interest is displayed. | - LEO is the reporter. | | | | | | | | | | |
| TC_RPT_060 | Reject report — report not found. | 1. Login as LEO.<br>2. Attempt to reject a non-existent report. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## LEO — Assign Team

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_061 | Assign team to verified report — success. | 1. Login as LEO.<br>2. Open a report with status "Verified".<br>3. Click "Phân công".<br>4. Select 1 cleanup team from the list.<br>5. Optionally add note.<br>6. Click "Xác nhận phân công". | Success message "Đã phân công team thành công." is displayed. Report status changes to "InProgress". Team receives notification. | - User is logged in as LEO.<br>- Report is Verified.<br>- At least 1 team exists with members. | | | | | | | | | | |
| TC_RPT_062 | Assign multiple teams to report. | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Select 2 cleanup teams.<br>4. Click "Xác nhận phân công". | Both teams are assigned. Report transitions to InProgress. Both team leaders receive notifications. | - User is logged in as LEO.<br>- Report is Verified.<br>- Multiple teams available. | | | | | | | | | | |
| TC_RPT_063 | Assign team — no teams selected. | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Click "Xác nhận phân công" without selecting any team. | Error message "Phải chọn ít nhất 1 team." is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_064 | Assign team — report not in Verified/Reopened status. | 1. Login as LEO.<br>2. Open a report with status "Submitted".<br>3. Attempt to assign a team. | Error message indicating invalid status is displayed. "Phân công" button should not be visible. | - User is logged in as LEO.<br>- Report is Submitted. | | | | | | | | | | |
| TC_RPT_065 | Assign team — report already assigned (InProgress). | 1. Login as LEO.<br>2. Open a report already in InProgress.<br>3. Attempt to assign another team. | Error message "Báo cáo đã được phân công." is displayed. | - User is logged in as LEO.<br>- Report is InProgress. | | | | | | | | | | |
| TC_RPT_066 | Assign team — team not found. | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Select a team that has been deleted or does not exist.<br>4. Click "Xác nhận phân công". | Error message "Không tìm thấy team" is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_067 | Assign team — team has no members. | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Select a team that has no active members.<br>4. Click "Xác nhận phân công". | Error message "Team chưa có thành viên" is displayed. | - User is logged in as LEO.<br>- Team exists but has no members. | | | | | | | | | | |
| TC_RPT_068 | Assign team — team workload exceeded (BR-OFF-013). | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Select a team that already has maximum number of active tasks.<br>4. Click "Xác nhận phân công". | Error message indicating team workload exceeded is displayed. | - User is logged in as LEO.<br>- Team has reached max task limit. | | | | | | | | | | |
| TC_RPT_069 | Assign team — cannot assign company team directly. | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Select a company team (not government team).<br>4. Click "Xác nhận phân công". | Error message "Không thể phân công trực tiếp cho team công ty" is displayed. LEO must use dispatch-to-company flow. | - User is logged in as LEO.<br>- Selected team is a company team. | | | | | | | | | | |
| TC_RPT_070 | Assign team — community cleanup event is active. | 1. Login as LEO.<br>2. Open a Verified report that has an active community cleanup event.<br>3. Attempt to assign a team. | Error message indicating community cleanup already active is displayed. | - User is logged in as LEO.<br>- Report has active community cleanup event. | | | | | | | | | | |

---

## LEO — Reassign Team

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_071 | Reassign team — success with valid reason. | 1. Login as LEO.<br>2. Open an InProgress report.<br>3. Click "Chuyển giao team".<br>4. Select old team and new team.<br>5. Enter reason ≥ 20 chars.<br>6. Confirm. | Success message "Đã chuyển giao team thành công." is displayed. Assignment transfers to new team. | - User is logged in as LEO.<br>- Report is InProgress with at least 1 assignment. | | | | | | | | | | |
| TC_RPT_072 | Reassign team — reason too short (< 20 chars). | 1. Login as LEO.<br>2. Open an InProgress report.<br>3. Click "Chuyển giao team".<br>4. Enter reason "Lý do ngắn".<br>5. Confirm. | Error message indicating reason too short is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## LEO — Escalate Report

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_073 | Escalate report to DEO — success (BR-ORG-016). | 1. Login as LEO.<br>2. Open a Verified report in LEO's ward.<br>3. Click "Chuyển lên DEO".<br>4. Enter reason ≥ 10 chars (e.g. "Báo cáo thuộc tuyến đường cấp thành phố, cần DEO phối hợp").<br>5. Confirm. | Success message "Đã chuyển báo cáo lên hàng đợi DEO." is displayed. Report moves to Department queue. | - User is logged in as LEO.<br>- Report is Verified or InProgress.<br>- Report is in LEO's office. | | | | | | | | | | |
| TC_RPT_074 | Escalate report — report not in Verified/InProgress status. | 1. Login as LEO.<br>2. Open a report with status "Submitted".<br>3. Attempt to escalate. | Error message "Trạng thái không hợp lệ" is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_075 | Escalate report — conflict of interest (reporter = LEO). | 1. Login as LEO who is the reporter.<br>2. Open the report.<br>3. Attempt to escalate. | Error message indicating conflict of interest is displayed. | - LEO is the reporter. | | | | | | | | | | |
| TC_RPT_076 | Escalate report — LEO not in same office (outside jurisdiction). | 1. Login as LEO from Ward A.<br>2. Open a report assigned to Ward B's office.<br>3. Attempt to escalate. | Error message "Ngoài quyền hạn" is displayed. | - LEO is from a different office. | | | | | | | | | | |
| TC_RPT_077 | Escalate report — report not found. | 1. Login as LEO.<br>2. Attempt to escalate a non-existent report ID. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## Company Dispatch & Assignment

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_078 | Dispatch report to company — success. | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Click "Điều phối đến công ty".<br>4. Select a valid, active company with valid contract.<br>5. Optionally add note.<br>6. Confirm. | Report transitions to InProgress. Company queue shows the report. | - User is logged in as LEO.<br>- Report is Verified.<br>- Company is active with valid contract. | | | | | | | | | | |
| TC_RPT_079 | Dispatch to company — company inactive or contract expired. | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Select a company with expired contract.<br>4. Confirm. | Error message "Công ty không hoạt động hoặc hết hợp đồng" is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_080 | CompanyManager assigns team — success. | 1. Login as CompanyManager.<br>2. Navigate to "Company Queue".<br>3. Open a report dispatched to the company.<br>4. Select company team(s).<br>5. Click "Phân công". | Success message "Đã phân công team công ty thành công." is displayed. Team assigned to the report. | - User is logged in as CompanyManager.<br>- Report was dispatched to this company. | | | | | | | | | | |
| TC_RPT_081 | CompanyManager assigns team — team not in company. | 1. Login as CompanyManager.<br>2. Select a team from another company.<br>3. Attempt to assign. | Error message "Team không thuộc công ty" is displayed. | - User is logged in as CompanyManager. | | | | | | | | | | |
| TC_RPT_082 | View company queue — CompanyManager sees pending tasks. | 1. Login as CompanyManager.<br>2. Navigate to "Hàng đợi công ty" page. | List of reports dispatched to the company, awaiting team assignment, is displayed with pagination. | - User is logged in as CompanyManager. | | | | | | | | | | |
| TC_RPT_083 | View company assignments — CompanyManager tracks progress. | 1. Login as CompanyManager.<br>2. Navigate to "Theo dõi task" page. | List of assigned tasks is displayed showing team name, progress %, assignment status, SLA deadline, and report thumbnail. | - User is logged in as CompanyManager. | | | | | | | | | | |
| TC_RPT_084 | View company report detail. | 1. Login as CompanyManager.<br>2. Click on a specific report in the assignment list. | Detail page shows report info, assigned teams with members, progress timeline, before/after images, and waste tags. | - User is logged in as CompanyManager. | | | | | | | | | | |

---

## Cleanup Team — Update Progress & Resolve

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_085 | Update progress — team leader updates % and note. | 1. Login as Cleaner (team leader).<br>2. Navigate to "Task của tôi".<br>3. Open an InProgress task.<br>4. Update progress to 50%.<br>5. Add note "Đã dọn xong 1/2 khu vực".<br>6. Click "Cập nhật". | Progress is updated to 50%. Note is saved. LEO can see the updated progress. | - User is logged in as team leader (Cleaner).<br>- Assignment is InProgress. | | | | | | | | | | |
| TC_RPT_086 | Update progress — with progress images. | 1. Login as Cleaner (team leader).<br>2. Open an InProgress task.<br>3. Update progress to 70%.<br>4. Upload 2 progress images.<br>5. Click "Cập nhật". | Progress updated with images. Images appear in the report timeline. | - User is logged in as team leader. | | | | | | | | | | |
| TC_RPT_087 | Update progress — user is not team leader. | 1. Login as a team member (not leader).<br>2. Open an InProgress task.<br>3. Attempt to update progress. | Error message "Bạn không phải team leader" is displayed. Update is blocked. | - User is logged in as team member but not leader. | | | | | | | | | | |
| TC_RPT_088 | Update progress — progress percent out of range. | 1. Login as Cleaner (team leader).<br>2. Open an InProgress task.<br>3. Enter progress = 150%.<br>4. Click "Cập nhật". | Error message "Phần trăm phải nằm trong khoảng 0–100" is displayed. | - User is logged in as team leader. | | | | | | | | | | |
| TC_RPT_089 | Upload before images — success (BR-REP-014). | 1. Login as Cleaner (team leader).<br>2. Open an InProgress task.<br>3. Click "Chụp ảnh hiện trạng".<br>4. Upload 2 before images.<br>5. Confirm. | Before images are saved. Images appear in the report detail as "Ảnh hiện trạng". | - User is logged in as team leader.<br>- Assignment is InProgress. | | | | | | | | | | |
| TC_RPT_090 | Resolve report — team completes assignment with ≥ 2 after images (BR-CLN-005). | 1. Login as Cleaner (team leader).<br>2. Open an InProgress task.<br>3. Click "Hoàn thành".<br>4. Upload 2 after (nghiệm thu) images.<br>5. Confirm completion. | Assignment status changes to Completed. If all teams completed, report transitions to "Resolved". | - User is logged in as team leader.<br>- Assignment is InProgress.<br>- Before images already uploaded. | | | | | | | | | | |
| TC_RPT_091 | Resolve report — less than 2 after images. | 1. Login as Cleaner (team leader).<br>2. Open an InProgress task.<br>3. Click "Hoàn thành".<br>4. Upload only 1 after image.<br>5. Confirm. | Error message "Cần ít nhất 2 ảnh nghiệm thu." is displayed. | - User is logged in as team leader. | | | | | | | | | | |
| TC_RPT_092 | Resolve report — missing before images (BR-REP-014). | 1. Login as Cleaner (team leader).<br>2. Open an InProgress task where no before images were uploaded.<br>3. Upload 2 after images.<br>4. Click "Hoàn thành". | Error message "Thiếu ảnh hiện trạng" is displayed. Must upload before images first. | - User is logged in as team leader.<br>- No before images uploaded. | | | | | | | | | | |
| TC_RPT_093 | Resolve report — user is not team leader. | 1. Login as a team member (not leader).<br>2. Open an InProgress task.<br>3. Attempt to resolve. | Error message "Bạn không phải team leader" is displayed. | - User is logged in as team member but not leader. | | | | | | | | | | |
| TC_RPT_094 | Resolve report — assignment not in InProgress status. | 1. Login as Cleaner (team leader).<br>2. Open a task that is already Completed.<br>3. Attempt to resolve again. | Error message "Trạng thái không hợp lệ" is displayed. | - User is logged in as team leader. | | | | | | | | | | |
| TC_RPT_095 | Resolve report — after image URL not owned by system. | 1. Login as Cleaner (team leader).<br>2. Open an InProgress task.<br>3. Provide after image URLs from an external domain.<br>4. Click "Hoàn thành". | Error message "URL ảnh không hợp lệ" is displayed. | - User is logged in as team leader. | | | | | | | | | | |
| TC_RPT_096 | Resolve report — partial completion (not all teams done). | 1. Login as Cleaner (team leader of Team A).<br>2. Complete Team A's assignment with 2 after images.<br>3. Team B's assignment is still InProgress. | Team A's assignment → Completed. Report stays InProgress (waiting for Team B). | - Two teams assigned to the report.<br>- Team B not yet completed. | | | | | | | | | | |

---

## Citizen — Close Report

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_097 | Close report — Citizen confirms satisfaction (BR-REP-016). | 1. Login as Citizen.<br>2. Navigate to "Báo cáo của tôi".<br>3. Open a report with status "Resolved".<br>4. Click "Đóng báo cáo". | Success message "Đã đóng báo cáo." is displayed. Report status changes to "Closed". | - User is logged in as Citizen.<br>- Report is Resolved. | | | | | | | | | | |
| TC_RPT_098 | Close report — report not in Resolved status. | 1. Login as Citizen.<br>2. Open a report with status "InProgress".<br>3. Attempt to click "Đóng báo cáo". | Error message "Trạng thái không hợp lệ" is displayed. Button should be hidden for non-Resolved reports. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_099 | Close report — not the reporter. | 1. Login as Citizen A.<br>2. Open a Resolved report created by Citizen B.<br>3. Attempt to close it. | Error message "Không phải người tạo báo cáo" is displayed. | - User is logged in as Citizen A. | | | | | | | | | | |
| TC_RPT_100 | Close report — report not found. | 1. Login as Citizen.<br>2. Attempt to close a non-existent report. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as Citizen. | | | | | | | | | | |

---

## Citizen — Rate Report (BR-REP-018)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_101 | Rate report — satisfied with 5 stars. | 1. Login as Citizen.<br>2. Open a Resolved or Closed report.<br>3. Click "Đánh giá".<br>4. Select "Hài lòng" and rate 5 stars.<br>5. Optionally add comment.<br>6. Submit rating. | Rating is saved. Confirmation message is displayed. | - User is logged in as Citizen.<br>- Report is Resolved or Closed.<br>- User is the reporter. | | | | | | | | | | |
| TC_RPT_102 | Rate report — not satisfied with 1 star and comment. | 1. Login as Citizen.<br>2. Open a Resolved report.<br>3. Select "Không hài lòng" and rate 1 star.<br>4. Add comment "Dọn chưa sạch, vẫn còn rác".<br>5. Submit rating. | Rating is saved with comment. | - User is logged in as Citizen.<br>- Report is Resolved or Closed. | | | | | | | | | | |
| TC_RPT_103 | Rate report — report not in Resolved/Closed status. | 1. Login as Citizen.<br>2. Open a report with status "InProgress".<br>3. Attempt to rate. | Error message "Trạng thái không hợp lệ" is displayed. Rating is not available. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_104 | Rate report — not the reporter. | 1. Login as Citizen A.<br>2. Open a Resolved report created by Citizen B.<br>3. Attempt to rate. | Error message "Không phải người tạo báo cáo" is displayed. | - User is logged in as Citizen A. | | | | | | | | | | |
| TC_RPT_105 | Rate report — already rated (one per report). | 1. Login as Citizen.<br>2. Open a report that has already been rated.<br>3. Attempt to rate again. | Error message "Bạn đã đánh giá báo cáo này rồi" is displayed. | - User has already rated this report. | | | | | | | | | | |
| TC_RPT_106 | Rate report — report not found. | 1. Login as Citizen.<br>2. Attempt to rate a non-existent report. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as Citizen. | | | | | | | | | | |

---

## Citizen — Request Reopen (BR-REP-015)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_107 | Request reopen — success with reason + images. | 1. Login as Citizen.<br>2. Open a Resolved report within 7 days.<br>3. Click "Yêu cầu mở lại".<br>4. Enter reason ≥ 20 chars (e.g. "Khu vực vẫn còn rác sau khi dọn, chưa sạch hoàn toàn").<br>5. Upload ≥ 1 evidence image.<br>6. Submit request. | Success message "Đã gửi yêu cầu mở lại." is displayed. Request appears in LEO's reopen queue. | - User is logged in as Citizen.<br>- Report is Resolved and within 7-day window. | | | | | | | | | | |
| TC_RPT_108 | Request reopen — no evidence images provided. | 1. Login as Citizen.<br>2. Open a Resolved report.<br>3. Click "Yêu cầu mở lại".<br>4. Enter valid reason but do not upload any images.<br>5. Submit. | Error message "Cần ít nhất 1 ảnh minh chứng" is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_109 | Request reopen — exceeded 7-day window. | 1. Login as Citizen.<br>2. Open a report that was Resolved more than 7 days ago.<br>3. Attempt to request reopen. | Error message "Đã quá 7 ngày kể từ khi giải quyết" is displayed. Reopen button is hidden. | - Report resolved > 7 days ago. | | | | | | | | | | |
| TC_RPT_110 | Request reopen — already reopened once (max limit). | 1. Login as Citizen.<br>2. Open a report that has already been reopened once.<br>3. Attempt to request another reopen. | Error message "Hết lượt mở lại" is displayed. | - Report already had 1 approved reopen. | | | | | | | | | | |
| TC_RPT_111 | Request reopen — not the reporter. | 1. Login as Citizen A.<br>2. Open a Resolved report by Citizen B.<br>3. Attempt to request reopen. | Error message "Không phải người tạo báo cáo" is displayed. | - User is not the report creator. | | | | | | | | | | |
| TC_RPT_112 | Request reopen — image URL not owned by system. | 1. Login as Citizen.<br>2. Open a Resolved report.<br>3. Enter valid reason and provide evidence image from external domain.<br>4. Submit. | Error message "URL ảnh không hợp lệ" is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_113 | Request reopen — with optional video URL. | 1. Login as Citizen.<br>2. Open a Resolved report within 7 days.<br>3. Enter reason ≥ 20 chars.<br>4. Upload 1 image and 1 video.<br>5. Submit. | Request is created with both image and video as evidence. | - User is logged in as Citizen. | | | | | | | | | | |

---

## LEO — Manage Reopen Requests

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_114 | View reopen requests queue. | 1. Login as LEO.<br>2. Navigate to "Yêu cầu mở lại" page. | List of pending reopen requests within LEO's office is displayed with report code, reason, and evidence images. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_115 | Approve reopen request — Resolved → Reopened. | 1. Login as LEO.<br>2. Open a pending reopen request.<br>3. Review evidence images and reason.<br>4. Click "Duyệt". | Reopen request approved. Report status changes Resolved → Reopened. LEO can reassign teams. | - User is logged in as LEO.<br>- Pending reopen request exists. | | | | | | | | | | |
| TC_RPT_116 | Reject reopen request — with valid reason (≥ 20 chars, BR-REP-022). | 1. Login as LEO.<br>2. Open a pending reopen request.<br>3. Click "Từ chối".<br>4. Enter reason ≥ 20 chars.<br>5. Confirm. | Reopen request rejected. Report stays Resolved. Citizen is notified of rejection. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_117 | Reject reopen request — reason too short. | 1. Login as LEO.<br>2. Open a pending reopen request.<br>3. Click "Từ chối".<br>4. Enter reason < 20 chars.<br>5. Confirm. | Error message "Lý do quá ngắn" is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## Citizen — Flag Report (BR-REP-033)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_118 | Flag report as duplicate — success. | 1. Login as Citizen.<br>2. Open a report created by another user.<br>3. Click "Gắn cờ" button.<br>4. Select type "Trùng lặp".<br>5. Optionally add reason.<br>6. Confirm. | Success message "Đã gắn cờ báo cáo." is displayed. | - User is logged in as Citizen.<br>- Report is not user's own. | | | | | | | | | | |
| TC_RPT_119 | Flag report — cannot flag own report. | 1. Login as Citizen.<br>2. Open a report created by the same user.<br>3. Attempt to click "Gắn cờ". | Error message "Không thể gắn cờ báo cáo của chính mình." is displayed. Flag button is hidden. | - User is logged in as Citizen.<br>- Report is user's own. | | | | | | | | | | |
| TC_RPT_120 | Flag report — already flagged same type (duplicate). | 1. Login as Citizen.<br>2. Open a report already flagged as "Trùng lặp" by this user.<br>3. Attempt to flag as "Trùng lặp" again. | Error message "Bạn đã gắn cờ báo cáo này rồi." is displayed. | - User already flagged this report with same type. | | | | | | | | | | |
| TC_RPT_121 | Flag report — threshold reached (3+ flags triggers LEO notification). | 1. Login as Citizen C (3rd user to flag).<br>2. Flag a report as "Spam".<br>3. This is the 3rd different user flagging same type. | Flag is saved. LEO receives notification "Báo cáo X đã nhận 3+ cờ Spam — cần xem xét". | - 2 other citizens already flagged same report same type. | | | | | | | | | | |
| TC_RPT_122 | Flag report — report not found. | 1. Login as Citizen.<br>2. Attempt to flag a non-existent report. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_123 | Flag report — user not logged in. | 1. Open a report detail page without logging in.<br>2. Attempt to flag. | User is redirected to Login page. | - User is not logged in. | | | | | | | | | | |

---

## Duplicate Detection (BR-REP-030..032)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_124 | View duplicate candidates list — LEO sees flagged reports. | 1. Login as LEO.<br>2. Navigate to "Nghi ngờ trùng lặp" page. | List of reports flagged as possible duplicates is displayed with reference to original report, thumbnail, and source (Geo/AI). | - User is logged in as LEO.<br>- Duplicate candidates exist in LEO's ward. | | | | | | | | | | |
| TC_RPT_125 | View duplicate candidate detail — side-by-side comparison. | 1. Login as LEO.<br>2. Click on a duplicate candidate from the list. | Side-by-side view shows current report vs primary report with images, distance, time difference, category, and AI similarity score. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_126 | Confirm duplicate — merge into primary (BR-REP-032). | 1. Login as LEO.<br>2. Open duplicate candidate detail.<br>3. Click "Xác nhận trùng lặp".<br>4. Select the primary report. | Report status changes to "Duplicate". Comments merged to primary. Reporter count incremented on primary. Duplicate reporter gets bonus points. | - User is logged in as LEO.<br>- Both reports exist. | | | | | | | | | | |
| TC_RPT_127 | Confirm duplicate — cannot merge report into itself. | 1. Login as LEO.<br>2. Attempt to confirm duplicate where primary = duplicate report. | Error message "Không thể gộp báo cáo vào chính nó." is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_128 | Confirm duplicate — primary report not in Verified/InProgress. | 1. Login as LEO.<br>2. Attempt to confirm duplicate where primary report is in Submitted status. | Error message "Trạng thái không hợp lệ" is displayed. Primary must be Verified/InProgress. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_129 | Dismiss duplicate flag (BR-REP-031). | 1. Login as LEO.<br>2. Open a duplicate candidate.<br>3. Click "Bác bỏ — không trùng lặp". | Duplicate flag removed. Report returns to normal queue. Success message "Đã bác bỏ cờ nghi ngờ trùng lặp." is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_130 | Dismiss duplicate — report has no duplicate flag. | 1. Login as LEO.<br>2. Attempt to dismiss duplicate on a report without duplicate flag. | Error message "Báo cáo không ở trạng thái nghi ngờ trùng lặp." is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## Violation Recurrence Detection (BR-REP-034)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_131 | View violation recurrence candidates. | 1. Login as LEO.<br>2. Navigate to "Nghi ngờ tái phạm" page. | List of reports flagged as suspected violation recurrence is displayed with reference to prior Closed report and timeline. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_132 | View violation recurrence comparison — side-by-side. | 1. Login as LEO.<br>2. Click on a recurrence candidate from the list. | Side-by-side comparison between current report and prior Closed report is shown with images and details. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_133 | Dismiss violation recurrence flag. | 1. Login as LEO.<br>2. Open a recurrence candidate.<br>3. Click "Bác bỏ — rác tái phát thông thường". | Success message "Đã bác bỏ cờ nghi ngờ vi phạm tái phát." is displayed. Flag is removed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_134 | Dismiss violation recurrence — report has no recurrence flag. | 1. Login as LEO.<br>2. Attempt to dismiss recurrence on a report without the flag. | Error message "Báo cáo không có cờ nghi ngờ vi phạm tái phát." is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## Waste Tags

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_135 | View waste tags list. | 1. Login as any authenticated user.<br>2. Navigate to report form or waste tag selector. | List of active waste tags is displayed sorted by display order. | - User is logged in. | | | | | | | | | | |
| TC_RPT_136 | LEO tags report with waste types — success. | 1. Login as LEO.<br>2. Open a report (Verified or InProgress).<br>3. Click "Gắn tag loại rác".<br>4. Select 1–12 tags.<br>5. Confirm. | Success message "Đã gắn tag loại rác thành công." is displayed. Tags appear on report detail. | - User is logged in as LEO.<br>- Report is Verified/InProgress. | | | | | | | | | | |
| TC_RPT_137 | Tag report — tag not found or inactive. | 1. Login as LEO.<br>2. Attempt to tag a report with a deactivated tag. | Error message indicating tag not found or inactive is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_138 | Tag report — invalid status. | 1. Login as LEO.<br>2. Attempt to tag a report in Closed status. | Error message "Trạng thái không hợp lệ" is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## Draft Management (BR-REP-019)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_139 | Save new draft — success. | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Fill partial report data.<br>4. Click "Lưu nháp". | Draft is saved. Success confirmation is shown. | - User is logged in as Citizen.<br>- User has < 3 drafts. | | | | | | | | | | |
| TC_RPT_140 | Save draft — update existing draft. | 1. Login as Citizen.<br>2. Open an existing draft from "Bản nháp".<br>3. Modify some fields.<br>4. Click "Lưu nháp". | Draft is updated with new data. | - User is logged in as Citizen.<br>- Draft exists. | | | | | | | | | | |
| TC_RPT_141 | Save draft — max 3 drafts limit reached. | 1. Login as Citizen who already has 3 drafts.<br>2. Navigate to "Tạo báo cáo" page.<br>3. Click "Lưu nháp" for a new draft. | Error message "Đã đạt giới hạn 3 bản nháp." is displayed. | - User already has 3 active drafts. | | | | | | | | | | |
| TC_RPT_142 | View my drafts. | 1. Login as Citizen.<br>2. Navigate to "Bản nháp" section. | List of user's drafts is displayed with last updated time. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_143 | Delete draft — success. | 1. Login as Citizen.<br>2. Open "Bản nháp" list.<br>3. Click "Xóa" on a draft.<br>4. Confirm deletion. | Draft is deleted. Success message is shown. Draft count decreases by 1. | - User is logged in as Citizen.<br>- Draft exists. | | | | | | | | | | |
| TC_RPT_144 | Delete draft — draft not found. | 1. Login as Citizen.<br>2. Attempt to delete a draft with non-existent ID. | Error message "Không tìm thấy bản nháp." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_145 | Update draft — draft belongs to another user. | 1. Login as Citizen A.<br>2. Attempt to update a draft ID that belongs to Citizen B. | Error message "Không tìm thấy bản nháp." is displayed. | - User is logged in as Citizen A. | | | | | | | | | | |

---

## LEO — Officer Queue

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_146 | View officer queue — LEO sees reports in their ward. | 1. Login as LEO.<br>2. Navigate to "Hàng đợi" page. | Reports in LEO's assigned ward are displayed: Submitted, Verified, Reopened. Sorted by priority score descending. | - User is logged in as LEO.<br>- Reports exist in LEO's ward. | | | | | | | | | | |
| TC_RPT_147 | Officer queue — filter by severity. | 1. Login as LEO.<br>2. Navigate to "Hàng đợi".<br>3. Select severity filter "Critical". | Only Critical severity reports are shown. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_148 | Officer queue — filter by SLA breached. | 1. Login as LEO.<br>2. Navigate to "Hàng đợi".<br>3. Toggle "Chỉ SLA đã vi phạm" filter. | Only reports that have breached their SLA deadline are shown. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_149 | Officer queue — search by report code. | 1. Login as LEO.<br>2. Navigate to "Hàng đợi".<br>3. Type a report code (e.g. "RPT-260809-ABC123") in search box. | Report matching the code is shown. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_150 | Officer queue — sort by created date ascending. | 1. Login as LEO.<br>2. Navigate to "Hàng đợi".<br>3. Change sort to "Ngày tạo" ascending. | Reports are ordered by creation date from oldest to newest. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_151 | Officer queue — filter by possible duplicate. | 1. Login as LEO.<br>2. Toggle "Nghi ngờ trùng lặp" filter. | Only reports flagged as possible duplicate are shown. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_152 | Officer queue — filter by pending reopen request. | 1. Login as LEO.<br>2. Toggle "Có yêu cầu mở lại" filter. | Only reports with pending reopen requests are shown. | - User is logged in as LEO. | | | | | | | | | | |

---

## LEO — Progress Board

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_153 | View progress board — InProgress reports as cards. | 1. Login as LEO.<br>2. Navigate to "Board tiến độ" page. | Grid of report cards showing: SLA countdown, overall progress %, top-3 team leader avatars. Negative hoursRemaining = SLA breached. | - User is logged in as LEO.<br>- InProgress reports exist in LEO's office. | | | | | | | | | | |
| TC_RPT_154 | Progress board — filter by severity. | 1. Login as LEO.<br>2. On progress board, select severity "High". | Only High severity InProgress reports are shown. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_155 | Progress board — filter SLA breached only. | 1. Login as LEO.<br>2. Toggle "SLA đã vi phạm" filter on progress board. | Only reports with breached SLA are shown (hoursRemaining < 0). | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_156 | View report progress detail. | 1. Login as LEO.<br>2. Click on a card in the progress board. | Detail page shows: team assignments, progress % per team, before/after images, progress images, SLA remaining, and status history. | - User is logged in as LEO. | | | | | | | | | | |

---

## Officer KPI (BR-OFF-021)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_157 | View own KPI — LEO sees personal stats. | 1. Login as LEO.<br>2. Navigate to "KPI" page. | KPI dashboard shows: verification on-time rate, resolution rate, average response time for the current period. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_158 | View KPI — DEO views specific officer's KPI. | 1. Login as DEO.<br>2. Navigate to "KPI" page.<br>3. Select a specific LEO from the dropdown. | Selected LEO's KPI data is displayed. | - User is logged in as DEO.<br>- LEO exists. | | | | | | | | | | |
| TC_RPT_159 | View KPI — custom date range. | 1. Login as LEO.<br>2. Navigate to "KPI" page.<br>3. Select custom date range (e.g. last 3 months). | KPI data is filtered to the selected date range. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_160 | View KPI — preset period (ThisMonth). | 1. Login as LEO.<br>2. Navigate to "KPI" page.<br>3. Select "Tháng này" period. | KPI data for the current month is displayed. | - User is logged in as LEO. | | | | | | | | | | |

---

## Export Reports (BR-OFF-022)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_161 | Export reports — CSV format. | 1. Login as LEO.<br>2. Navigate to "Báo cáo" page.<br>3. Click "Xuất báo cáo".<br>4. Select format "CSV".<br>5. Click "Tải xuống". | CSV file is downloaded with report data. File contains columns for code, status, severity, category, address, created date, duplicate flag, violation recurrence flag. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_162 | Export reports — Excel format. | 1. Login as LEO.<br>2. Navigate to "Báo cáo" page.<br>3. Click "Xuất báo cáo".<br>4. Select format "Excel".<br>5. Click "Tải xuống". | Excel file is downloaded with report data. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_163 | Export reports — with filters applied. | 1. Login as LEO.<br>2. Apply filters: status = Verified, severity = High.<br>3. Click "Xuất báo cáo". | Exported file contains only Verified + High severity reports. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_164 | Export reports — Admin sees PII. | 1. Login as Admin.<br>2. Export reports. | Exported file includes PII columns (reporter name, email). | - User is logged in as Admin. | | | | | | | | | | |
| TC_RPT_165 | Export reports — LEO does not see PII. | 1. Login as LEO.<br>2. Export reports. | Exported file does NOT include PII columns. Reporter data is hidden. | - User is logged in as LEO. | | | | | | | | | | |

---

## Inspection Report (Nested under Reports)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_166 | Create inspection report — success (BR-INS-001). | 1. Login as LEO.<br>2. Open a Verified report.<br>3. Click "Lập hồ sơ xử phạt".<br>4. Fill violation description, violator name, address, identity.<br>5. Optionally assign inspection team.<br>6. Confirm. | Inspection report (Draft) is created. Success message is displayed. | - User is logged in as LEO.<br>- Report is Verified. | | | | | | | | | | |
| TC_RPT_167 | Create inspection — report not found. | 1. Login as LEO.<br>2. Attempt to create inspection for a non-existent report. | Error message "Không tìm thấy báo cáo" is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_168 | Create inspection — report already has active inspection (409 Conflict). | 1. Login as LEO.<br>2. Open a report that already has an active inspection.<br>3. Attempt to create another. | Error message "Đã có hồ sơ xử phạt đang hoạt động" is displayed. | - User is logged in as LEO.<br>- Active inspection exists. | | | | | | | | | | |
| TC_RPT_169 | Create inspection — report not Verified. | 1. Login as LEO.<br>2. Open a report with status Submitted.<br>3. Attempt to create inspection. | Error message "Báo cáo chưa Verified." is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_170 | View inspections by report. | 1. Login as LEO or Inspector.<br>2. Open a report detail.<br>3. Click "Hồ sơ xử phạt" tab. | List of inspection reports for this report is displayed. | - User is logged in as LEO/Inspector. | | | | | | | | | | |

---

## Authorization & Access Control

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_171 | Citizen cannot verify report. | 1. Login as Citizen.<br>2. Open a Submitted report.<br>3. Attempt to access "Xác minh" action. | "Xác minh" button is not visible. If accessed directly, 403 Forbidden is returned. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_172 | Citizen cannot reject report. | 1. Login as Citizen.<br>2. Attempt to reject a report. | "Từ chối" action is not available. 403 Forbidden if accessed directly. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_173 | Citizen cannot assign team. | 1. Login as Citizen.<br>2. Attempt to assign a team to a report. | "Phân công" action is not available. 403 Forbidden if accessed directly. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_174 | Citizen cannot export reports. | 1. Login as Citizen.<br>2. Attempt to access export endpoint. | 403 Forbidden is returned. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_175 | Cleaner cannot verify report. | 1. Login as Cleaner.<br>2. Attempt to access verify endpoint. | 403 Forbidden is returned. | - User is logged in as Cleaner. | | | | | | | | | | |
| TC_RPT_176 | Cleaner cannot submit report. | 1. Login as Cleaner.<br>2. Attempt to access submit report endpoint. | 403 Forbidden is returned. | - User is logged in as Cleaner. | | | | | | | | | | |
| TC_RPT_177 | CompanyStaff cannot access LEO dashboard. | 1. Login as CompanyStaff.<br>2. Attempt to access officer queue endpoint. | 403 Forbidden is returned. | - User is logged in as CompanyStaff. | | | | | | | | | | |
| TC_RPT_178 | Unauthenticated user cannot view reports. | 1. Open reports list page without logging in. | User is redirected to Login page or 401 Unauthorized is returned. | - User is not logged in. | | | | | | | | | | |
| TC_RPT_179 | CompanyManager cannot verify/reject/escalate reports. | 1. Login as CompanyManager.<br>2. Attempt to access verify, reject, or escalate endpoints. | 403 Forbidden is returned for all three actions. | - User is logged in as CompanyManager. | | | | | | | | | | |
| TC_RPT_180 | Admin can access all report operations. | 1. Login as Admin.<br>2. Navigate to verify, reject, assign, escalate, export features. | All actions are accessible. Admin role has full permissions. | - User is logged in as Admin. | | | | | | | | | | |

---

## Edge Cases & Boundary Tests

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_RPT_181 | Submit report — latitude at boundary (8.0). | 1. Login as Citizen.<br>2. Submit a report with latitude = 8.0. | Report is created successfully (8.0 is valid boundary). | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_182 | Submit report — latitude at boundary (24.0). | 1. Login as Citizen.<br>2. Submit a report with latitude = 24.0. | Report is created successfully (24.0 is valid boundary). | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_183 | Submit report — longitude at boundary (102.0). | 1. Login as Citizen.<br>2. Submit a report with longitude = 102.0. | Report is created successfully (102.0 is valid boundary). | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_184 | Submit report — longitude at boundary (110.0). | 1. Login as Citizen.<br>2. Submit a report with longitude = 110.0. | Report is created successfully (110.0 is valid boundary). | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_185 | Submit report — description exactly 10 chars. | 1. Login as Citizen.<br>2. Submit report with description exactly 10 characters. | Report is created successfully. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_186 | Submit report — description exactly 1000 chars. | 1. Login as Citizen.<br>2. Submit report with description exactly 1000 characters. | Report is created successfully. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_187 | Submit report — exactly 5 images (max). | 1. Login as Citizen.<br>2. Upload exactly 5 images.<br>3. Submit report. | Report is created with all 5 images. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_188 | Reject report — reason exactly 20 chars (boundary). | 1. Login as LEO.<br>2. Reject a Submitted report with reason exactly 20 characters. | Report is rejected successfully. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_189 | Reject report — reason 19 chars (boundary fail). | 1. Login as LEO.<br>2. Reject a Submitted report with reason of 19 characters. | Error message "Lý do quá ngắn" is displayed. | - User is logged in as LEO. | | | | | | | | | | |
| TC_RPT_190 | Rate limit — submit 5 reports/hour (boundary). | 1. Login as Citizen.<br>2. Submit exactly 5 reports within 1 hour. | All 5 reports are accepted. No rate limit error. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_191 | Draft limit — save 3 drafts (boundary). | 1. Login as Citizen with 0 drafts.<br>2. Save draft 1, 2, 3 sequentially. | All 3 drafts are saved successfully. | - User starts with 0 drafts. | | | | | | | | | | |
| TC_RPT_192 | Waste tags — maximum 10 tags per report. | 1. Login as Citizen.<br>2. Submit report with exactly 10 waste tags. | Report is created with all 10 tags. | - 10 active waste tags exist. | | | | | | | | | | |
| TC_RPT_193 | Waste tags — 11 tags (exceeds limit). | 1. Login as Citizen.<br>2. Attempt to submit report with 11 waste tags. | Error message "Tối đa 10 waste tags mỗi báo cáo." is displayed. | - 11+ active waste tags exist. | | | | | | | | | | |
| TC_RPT_194 | Waste tags — duplicate tag IDs. | 1. Login as Citizen.<br>2. Submit report with same waste tag ID twice. | Error message "Danh sách waste tags không được trùng lặp." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_195 | ProvinceCode format — not 2-digit. | 1. Login as Citizen.<br>2. Submit report with ProvinceCode = "ABC". | Error message "ProvinceCode must be a 2-digit official code when provided." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |
| TC_RPT_196 | WardCode format — not 5-digit. | 1. Login as Citizen.<br>2. Submit report with WardCode = "1234". | Error message "WardCode must be a 5-digit official code when provided." is displayed. | - User is logged in as Citizen. | | | | | | | | | | |

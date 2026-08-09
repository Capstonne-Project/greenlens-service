# Test Report — Media

|                      |                              |
| -------------------- | ---------------------------- |
| **Feature**          | **Media — File Upload**      |
| **Test requirement** |                              |
| **Number of TCs**    | **62**                       |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 61     | 1      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## Presign Direct R2 Upload — ReportImage

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_001 | Presign upload — ReportImage (JPEG). | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo".<br>3. Chọn ảnh JPEG từ thiết bị.<br>4. App gọi presign API với purpose=ReportImage. | Upload URL và public URL được trả về. URL có thời hạn 15 phút. MaxBytes = 10MB. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler resolves folder "reports/images", maxBytes 10MB, TTL 15 phút. |
| TC_MED_002 | Presign upload — ReportImage (PNG). | 1. Login as Citizen.<br>2. Chọn ảnh PNG.<br>3. App gọi presign API. | Upload URL và public URL được trả về. Content-Type = image/png. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | ReportImageContentTypes allows PNG. |
| TC_MED_003 | Presign upload — ReportImage (WebP). | 1. Login as Citizen.<br>2. Chọn ảnh WebP.<br>3. App gọi presign API. | Upload URL được trả về. Content-Type = image/webp. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | ReportImageContentTypes allows WebP. |
| TC_MED_004 | Presign upload — ReportImage (HEIC from iPhone). | 1. Login as Citizen trên iPhone.<br>2. Chọn ảnh HEIC.<br>3. App gọi presign API. | Upload URL được trả về. Content-Type resolved via extension fallback. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | ReportImageContentTypes.TryResolve handles HEIC/octet-stream fallback. |
| TC_MED_005 | Presign upload — file vượt 10MB. | 1. Login as Citizen.<br>2. Chọn ảnh > 10MB.<br>3. App gọi presign API. | Error "Ảnh quá lớn" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `size > maxBytes (10MB)` → `Errors.Media.ImageTooLarge`. |
| TC_MED_006 | Presign upload — invalid MIME (PDF). | 1. Login as Citizen.<br>2. Chọn file PDF.<br>3. App gọi presign API. | Error "Loại ảnh không hợp lệ" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `ReportImageContentTypes.TryResolve` rejects non-image types. |
| TC_MED_007 | Presign upload — empty FileName. | 1. Login as Citizen.<br>2. Gọi presign API với FileName trống. | Error "FileName is required." is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for FileName. |
| TC_MED_008 | Presign upload — FileName quá dài (> 200 chars). | 1. Login as Citizen.<br>2. Gọi presign API với FileName > 200 ký tự. | Error indicating max length exceeded is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(200)` for FileName. |
| TC_MED_009 | Presign upload — empty ContentType. | 1. Login as Citizen.<br>2. Gọi presign API với ContentType trống. | Error indicating content type required is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for ContentType. |

---

## Presign Direct R2 Upload — Comment

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_010 | Presign upload — Comment (JPEG, < 5MB). | 1. Login as any user.<br>2. Navigate to bình luận của một báo cáo.<br>3. Click "Đính kèm ảnh".<br>4. Chọn ảnh JPEG < 5MB. | Upload URL được trả về. Folder = "comments/images". MaxBytes = 5MB. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler resolves folder "comments/images", maxBytes 5MB. |
| TC_MED_011 | Presign upload — Comment image > 5MB. | 1. Login as any user.<br>2. Chọn ảnh > 5MB cho bình luận. | Error "Ảnh quá lớn" is displayed. Max 5MB for comment. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `size > 5MB` → `Errors.Media.ImageTooLarge`. |

---

## Presign Direct R2 Upload — Avatar

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_012 | Presign upload — Avatar (JPEG, < 5MB). | 1. Login as any user.<br>2. Navigate to "Chỉnh sửa hồ sơ".<br>3. Click "Đổi ảnh đại diện".<br>4. Chọn ảnh JPEG < 5MB. | Upload URL được trả về. Folder = "users/avatars". MaxBytes = 5MB. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler resolves folder "users/avatars", maxBytes 5MB. |
| TC_MED_013 | Presign upload — Avatar > 5MB. | 1. Login as any user.<br>2. Chọn ảnh > 5MB cho avatar. | Error "Ảnh quá lớn" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `size > 5MB` → `Errors.Media.ImageTooLarge`. |

---

## Presign Direct R2 Upload — Before/Progress/After (Cleanup)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_014 | Presign upload — Before (Cleaner, < 20MB). | 1. Login as Cleaner.<br>2. Mở task cleanup.<br>3. Upload ảnh "before" (trước dọn). | Upload URL được trả về. Folder = "reports/{reportId}/before". MaxBytes = 20MB. | - User is Cleaner.<br>- ReportId provided. | Passed | 04/09/2026 | TamKnm | | | | | | | Before purpose requires ReportId. maxBytes 20MB. |
| TC_MED_015 | Presign upload — Before without ReportId. | 1. Login as Cleaner.<br>2. Gọi presign API purpose=Before nhưng không truyền ReportId. | Error "ReportId is required for Before/Progress/ReopenEvidence uploads." is displayed. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` when Purpose is Before. |
| TC_MED_016 | Presign upload — Progress (Cleaner). | 1. Login as Cleaner.<br>2. Upload ảnh tiến độ. | Upload URL được trả về. Folder = "reports/{reportId}/progress". MaxBytes = 20MB. | - User is Cleaner.<br>- ReportId provided. | Passed | 04/09/2026 | TamKnm | | | | | | | Progress purpose resolves to "reports/{reportId}/progress". |
| TC_MED_017 | Presign upload — After (Cleaner). | 1. Login as Cleaner.<br>2. Upload ảnh "after" (sau dọn). | Upload URL được trả về. Folder = "reports/images". MaxBytes = 10MB. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | After purpose resolves to "reports/images", 10MB limit. |
| TC_MED_018 | Before/After — Citizen cannot upload. | 1. Login as Citizen.<br>2. Gọi presign API với purpose=Before. | Error "Mục đích upload không được phép" is displayed. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `CanUploadPurpose` restricts Before/After to Cleaner/CompanyStaff only. |
| TC_MED_019 | Progress — Citizen cannot upload. | 1. Login as Citizen.<br>2. Gọi presign API với purpose=Progress. | Error "Mục đích upload không được phép" is displayed. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `CanUploadPurpose` restricts Progress to Cleaner/CompanyStaff/Inspector. |

---

## Presign Direct R2 Upload — ReopenEvidence

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_020 | Presign upload — ReopenEvidence success. | 1. Login as Citizen (report owner).<br>2. Mở báo cáo đã Resolved.<br>3. Click "Yêu cầu mở lại".<br>4. Chọn ảnh bằng chứng. | Upload URL được trả về. Folder = "reports/{reportId}/reopen". MaxBytes = 10MB. | - User owns the report.<br>- Report is Resolved. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates report ownership and reopen eligibility. |
| TC_MED_021 | ReopenEvidence — report not found. | 1. Login as Citizen.<br>2. Gọi presign với purpose=ReopenEvidence và ReportId không tồn tại. | Error "Không tìm thấy báo cáo" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateReopenEvidenceUploadAsync` checks report exists. |
| TC_MED_022 | ReopenEvidence — not report owner. | 1. Login as Citizen A.<br>2. Cố upload reopen evidence cho report của Citizen B. | Error "Bạn không phải người báo cáo" is displayed. | - User is not the reporter. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `report.ReporterId != currentUser.UserId`. |
| TC_MED_023 | ReopenEvidence — without ReportId. | 1. Login as Citizen.<br>2. Gọi presign với purpose=ReopenEvidence nhưng không có ReportId. | Error "ReportId is required" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` when Purpose is ReopenEvidence. |

---

## Presign Direct R2 Upload — InspectionEvidence

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_024 | Presign upload — InspectionEvidence ScenePhoto. | 1. Login as Inspector.<br>2. Mở hồ sơ xử phạt InProgress.<br>3. Chọn ảnh hiện trường.<br>4. App gọi presign với purpose=InspectionEvidence, category=ScenePhoto. | Upload URL được trả về. Folder = "reports/{reportId}/inspection/{inspectionId}/scenephoto". | - User is Inspector in assigned team.<br>- Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates team membership and inspection status before generating URL. |
| TC_MED_025 | Presign upload — InspectionEvidence Video. | 1. Login as Inspector.<br>2. Upload video bằng chứng (mp4). | Upload URL được trả về cho video. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Category Video allows video/* content types. |
| TC_MED_026 | Presign upload — InspectionEvidence Audio. | 1. Login as Inspector.<br>2. Upload ghi âm (audio). | Upload URL được trả về cho audio. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Category Audio allows audio/* content types. |
| TC_MED_027 | InspectionEvidence — ViolationStatus category rejected. | 1. Login as Inspector.<br>2. Gọi presign với category=ViolationStatus. | Error "ViolationStatus là text — dùng PUT /checklist" is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator and handler both reject ViolationStatus category. |
| TC_MED_028 | InspectionEvidence — without InspectionId. | 1. Login as Inspector.<br>2. Gọi presign với purpose=InspectionEvidence nhưng không có InspectionId. | Error "InspectionId is required for InspectionEvidence uploads." is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` when Purpose is InspectionEvidence. |
| TC_MED_029 | InspectionEvidence — inspection not found. | 1. Login as Inspector.<br>2. Gọi presign với InspectionId không tồn tại. | Error "Không tìm thấy hồ sơ" is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateInspectionEvidenceUploadAsync` checks inspection exists. |
| TC_MED_030 | InspectionEvidence — field report already submitted. | 1. Login as Inspector.<br>2. Cố upload evidence sau khi biên bản đã nộp. | Error "Biên bản đã nộp, không thể thay đổi" is displayed. | - Field investigation already submitted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection.FieldInvestigationSubmittedAt.HasValue`. |
| TC_MED_031 | InspectionEvidence — inspection not InProgress. | 1. Login as Inspector.<br>2. Cố upload evidence cho hồ sơ ở trạng thái Draft. | Error "Trạng thái không hợp lệ" is displayed. | - Inspection is Draft. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection.Status != InspectionStatus.InProgress`. |
| TC_MED_032 | InspectionEvidence — Citizen cannot upload. | 1. Login as Citizen.<br>2. Gọi presign với purpose=InspectionEvidence. | Error "Mục đích upload không được phép" is displayed. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `CanUploadPurpose` restricts InspectionEvidence to Inspector only. |
| TC_MED_033 | InspectionEvidence — not in assigned team. | 1. Login as Inspector ở Team A.<br>2. Cố upload evidence cho hồ sơ của Team B. | Error "Bạn không thuộc team được gán" is displayed. | - User not in assigned team. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamMemberAsync` checks team membership. |

---

## Presign — Error & Edge Cases

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_034 | Presign — invalid purpose enum value. | 1. Login as any user.<br>2. Gọi presign API với purpose = 999 (invalid). | Error "Purpose is invalid" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `IsInEnum()` for Purpose. |
| TC_MED_035 | Presign — R2 service unavailable. | 1. Login as any user.<br>2. Gọi presign API khi R2 Cloudflare bị down. | Error "Upload thất bại" is displayed. | - R2 Cloudflare is down. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches `Exception` from `fileStorage.CreatePresignedUploadAsync` → `Errors.Users.StorageUploadFailed`. |
| TC_MED_036 | Presign — FileSizeBytes = 0 (invalid). | 1. Login as any user.<br>2. Gọi presign với FileSizeBytes = 0. | Error "File size must be greater than 0" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `GreaterThan(0)` when FileSizeBytes has value. |
| TC_MED_037 | Presign — not logged in. | 1. Gọi presign API mà không có JWT token. | 401 Unauthorized is returned. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` on class level. |

---

## Upload Report Image (Legacy — Deprecated)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_038 | Upload report image — success (JPEG < 10MB). | 1. Login as Citizen.<br>2. Mở giao diện tạo báo cáo.<br>3. Chọn ảnh JPEG < 10MB.<br>4. Upload qua endpoint legacy. | Success. URL ảnh được trả về. Message "Tải ảnh báo cáo thành công." | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `[Obsolete]` nhưng vẫn hoạt động cho rollback. Max 10MB. |
| TC_MED_039 | Upload report image — no file selected. | 1. Login as Citizen.<br>2. Submit form mà không chọn file. | Error "Vui lòng chọn file ảnh." with status 400. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller checks `file is null || file.Length == 0`. |
| TC_MED_040 | Upload report image — file > 10MB. | 1. Login as Citizen.<br>2. Chọn ảnh > 10MB. | Error "Ảnh quá lớn" is displayed. Max 10MB. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `request.FileSize > 10MB` → `Errors.Media.ImageTooLarge`. |
| TC_MED_041 | Upload report image — invalid MIME (GIF). | 1. Login as Citizen.<br>2. Chọn file GIF. | Error "Loại ảnh không hợp lệ" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `ReportImageContentTypes.TryResolve` rejects GIF. |
| TC_MED_042 | Upload report image — R2 upload fails. | 1. Login as Citizen.<br>2. Chọn ảnh hợp lệ.<br>3. Upload khi R2 bị lỗi. | Error "Upload thất bại" is displayed. | - R2 is down. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches Exception → `Errors.Users.StorageUploadFailed`. |
| TC_MED_043 | Upload report image — FileName with full path (sanitized). | 1. Login as Citizen.<br>2. Upload ảnh từ client gửi full path (e.g. "C:\Users\test\photo.jpg"). | Upload thành công. Chỉ filename "photo.jpg" được sử dụng. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `Path.GetFileName(file.FileName)` strips path segments. |

---

## Upload Comment Image

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_044 | Upload comment image — success (JPEG < 5MB). | 1. Login as any user.<br>2. Mở bình luận của một báo cáo.<br>3. Click "Đính kèm ảnh".<br>4. Chọn ảnh JPEG < 5MB.<br>5. Upload. | Success. URL ảnh được trả về. Message "Tải ảnh bình luận thành công." | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Max 5MB. Folder "comments/images". |
| TC_MED_045 | Upload comment image — no file selected. | 1. Login as any user.<br>2. Submit bình luận có đính kèm ảnh nhưng không chọn file. | Error "Vui lòng chọn file ảnh." with status 400. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller checks `file is null || file.Length == 0`. |
| TC_MED_046 | Upload comment image — file > 5MB. | 1. Login as any user.<br>2. Chọn ảnh > 5MB cho bình luận. | Error "Ảnh bình luận quá lớn" is displayed. Max 5MB. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `request.FileSize > 5MB` → `Errors.Comments.CommentImageTooLarge`. |
| TC_MED_047 | Upload comment image — invalid MIME. | 1. Login as any user.<br>2. Chọn file PDF cho bình luận. | Error "Loại ảnh không hợp lệ" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `ReportImageContentTypes.TryResolve` rejects non-image. |
| TC_MED_048 | Upload comment image — R2 upload fails. | 1. Login as any user.<br>2. Upload khi R2 bị lỗi. | Error "Upload thất bại" is displayed. | - R2 is down. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches Exception → `Errors.Users.StorageUploadFailed`. |

---

## Upload Report Video

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_049 | Upload report video — MP4 success (< 100MB, < 60s). | 1. Login as Citizen.<br>2. Navigate to "Tạo báo cáo".<br>3. Click "Quay/Chọn video".<br>4. Chọn file MP4 < 100MB, thời lượng < 60s.<br>5. Upload. | Success. Video compressed (H.264 720p CRF 28). Response shows: original size, compressed size, duration, width, height. Message "Tải video báo cáo thành công." | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Server-side transcode with H.264, 720p, CRF 28, AAC 96k, faststart. |
| TC_MED_050 | Upload report video — MOV file. | 1. Login as Citizen.<br>2. Chọn file MOV (from iPhone). | Success. Video transcoded to MP4. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | ContentType "video/quicktime" allowed. Output always .mp4. |
| TC_MED_051 | Upload report video — no file selected. | 1. Login as Citizen.<br>2. Submit mà không chọn video. | Error "Vui lòng chọn file video." with status 400. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller checks `file is null || file.Length == 0`. |
| TC_MED_052 | Upload report video — file > 100MB. | 1. Login as Citizen.<br>2. Chọn video > 100MB. | Error "Video quá lớn" is displayed. Max 100MB. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `request.FileSize > 100MB` → `Errors.Media.VideoTooLarge`. |
| TC_MED_053 | Upload report video — invalid MIME (AVI). | 1. Login as Citizen.<br>2. Chọn file AVI. | Error "Loại video không hợp lệ" is displayed. Chỉ MP4 và MOV được chấp nhận. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `AllowedContentTypes` only includes "video/mp4" and "video/quicktime". |
| TC_MED_054 | Upload report video — duration exceeds 60 seconds. | 1. Login as Citizen.<br>2. Chọn video dài hơn 60 giây. | Error "Video vượt quá thời lượng cho phép" is displayed. Max 60s. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches `VideoDurationExceededException` → `Errors.Media.VideoDurationExceeded`. |
| TC_MED_055 | Upload report video — transcode fails. | 1. Login as Citizen.<br>2. Chọn video hợp lệ.<br>3. Upload khi FFmpeg bị lỗi. | Error "Nén video thất bại" is displayed. | - FFmpeg unavailable or corrupt video. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches generic Exception from transcode → `Errors.Media.VideoTranscodeFailed`. |
| TC_MED_056 | Upload report video — R2 upload fails after transcode. | 1. Login as Citizen.<br>2. Chọn video hợp lệ.<br>3. Transcode thành công nhưng R2 bị lỗi. | Error "Upload thất bại" is displayed. | - R2 is down. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler catches Exception from `fileStorage.UploadAsync` → `Errors.Users.StorageUploadFailed`. |
| TC_MED_057 | Upload report video — compression ratio shown. | 1. Login as Citizen.<br>2. Upload video MP4 50MB. | Response shows originalSizeBytes, compressedSizeBytes, and reduced file size. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Response includes both sizes for transparency. |

---

## Role-Based Access Control

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_MED_058 | All media endpoints require authentication. | 1. Gọi POST /v1/media/presign, POST /v1/media/reports/images, POST /v1/media/comments/images, POST /v1/media/reports/videos mà không có JWT. | Tất cả trả về 401 Unauthorized. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller `[Authorize]` on class level. |
| TC_MED_059 | Admin can upload any purpose. | 1. Login as Admin.<br>2. Gọi presign với mỗi purpose: ReportImage, Before, After, Progress, Comment, Avatar, ReopenEvidence, InspectionEvidence. | Tất cả đều thành công. | - User is Admin. | Passed | 04/09/2026 | TamKnm | | | | | | | `CanUploadPurpose` returns true for Admin on all purposes. |
| TC_MED_060 | Citizen can upload ReportImage, Comment, Avatar, ReopenEvidence. | 1. Login as Citizen.<br>2. Gọi presign với purpose=ReportImage, Comment, Avatar, ReopenEvidence. | Tất cả đều thành công. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `CanUploadPurpose` allows these for any role. |
| TC_MED_061 | Citizen cannot upload Before, After, Progress, InspectionEvidence. | 1. Login as Citizen.<br>2. Gọi presign lần lượt với purpose=Before, After, Progress, InspectionEvidence. | Tất cả trả về error "Mục đích upload không được phép". | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `CanUploadPurpose` restricts these to specific roles. |
| TC_MED_062 | InspectionEvidence — invalid content type for ScenePhoto (video file). | 1. Login as Inspector.<br>2. Gọi presign với category=ScenePhoto và contentType=video/mp4. | Error "Loại ảnh không hợp lệ" is displayed. ScenePhoto chỉ chấp nhận image types. | - User is Inspector. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: Khi Inspector gửi presign với ScenePhoto + video content type, handler trả về generic `Errors.Media.InvalidImageType` thay vì message rõ ràng hơn như "ScenePhoto chỉ chấp nhận ảnh (JPEG, PNG, WebP, HEIC)". Error message không giúp user hiểu đúng loại file nào được chấp nhận cho category này. |

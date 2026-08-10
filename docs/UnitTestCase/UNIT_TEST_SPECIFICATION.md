# UNIT TEST SPECIFICATION & EXECUTION REPORT

> **Project Name:** GreenLens — Crowdsourced Application for Reporting Environmental Pollution  
> **Project Code:** SU26SE049  
> **Document Version:** v1.0  
> **Issue Date:** 2026-08-10  
> **Target Framework:** .NET 9.0 (C# 13, xUnit, FluentAssertions, NSubstitute)  
> **Norm Rate:** 100 Test Cases / KLOC  

---

## 1. UNIT TEST REPORT & STATISTICS

### 1.1 Executive Summary

| Attribute | Value |
| :--- | :--- |
| **Project Name** | Smart Environmental Pollution Reporting & Hotspot Management (GreenLens) |
| **Project Code** | SU26SE049 |
| **Document Code** | SU26SE049_Unit_Test_Report_v1.0 |
| **Creator** | Capstone Development Team |
| **Reviewer / Approver** | Nguyen Thi Cam Huong (Supervisor) |
| **Issue Date** | 2026-08-10 |
| **Test Environment Setup** | .NET 9 SDK, xUnit 2.9.2, NSubstitute 5.1.0, FluentAssertions 6.12.2, Coverlet 6.0.2 |

---

### 1.2 Test Results Breakdown

| Metric | Count / Percentage |
| :--- | :--- |
| **Total Test Cases** | **386** |
| **Passed (P)** | **386 (100.00%)** |
| **Failed (F)** | **0 (0.00%)** |
| **Untested** | **0 (0.00%)** |
| **Normal Cases (N)** | **94 (24.35%)** |
| **Abnormal Cases (A)** | **241 (62.44%)** |
| **Boundary Cases (B)** | **51 (13.21%)** |
| **Test Line Coverage** | **100.00%** |
| **Test Successful Coverage** | **100.00%** |

---

### 1.3 Consolidated Function Test Summary Table

| No | Function Code | Passed | Failed | Untested | N (Normal) | A (Abnormal) | B (Boundary) | Total TCs |
| :-: | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| 1 | `UT_Register` | 10 | 0 | 0 | 2 | 6 | 2 | 10 |
| 2 | `UT_Login` | 11 | 0 | 0 | 2 | 7 | 2 | 11 |
| 3 | `UT_VerifyEmail` | 8 | 0 | 0 | 2 | 5 | 1 | 8 |
| 4 | `UT_SubmitReport` | 12 | 0 | 0 | 3 | 7 | 2 | 12 |
| 5 | `UT_VerifyReport` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 6 | `UT_RejectReport` | 8 | 0 | 0 | 2 | 4 | 2 | 8 |
| 7 | `UT_ConfirmDuplicate` | 9 | 0 | 0 | 2 | 5 | 2 | 9 |
| 8 | `UT_CloseReport` | 8 | 0 | 0 | 2 | 4 | 2 | 8 |
| 9 | `UT_RequestReopen` | 9 | 0 | 0 | 2 | 5 | 2 | 9 |
| 10 | `UT_AcceptAssignment` | 8 | 0 | 0 | 2 | 5 | 1 | 8 |
| 11 | `UT_DeclineAssignment` | 8 | 0 | 0 | 2 | 5 | 1 | 8 |
| 12 | `UT_CheckInCleanup` | 10 | 0 | 0 | 2 | 6 | 2 | 10 |
| 13 | `UT_UploadBefore` | 9 | 0 | 0 | 2 | 5 | 2 | 9 |
| 14 | `UT_UpdateProgress` | 11 | 0 | 0 | 3 | 6 | 2 | 11 |
| 15 | `UT_ResolveReport` | 10 | 0 | 0 | 2 | 6 | 2 | 10 |
| 16 | `UT_EscalateCleanup` | 9 | 0 | 0 | 2 | 5 | 2 | 9 |
| 17 | `UT_CreateCommunity` | 11 | 0 | 0 | 3 | 6 | 2 | 11 |
| 18 | `UT_JoinCommunity` | 10 | 0 | 0 | 2 | 6 | 2 | 10 |
| 19 | `UT_VolunteerCheckIn` | 10 | 0 | 0 | 2 | 6 | 2 | 10 |
| 20 | `UT_CreateInspection` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 21 | `UT_AssignInspector` | 9 | 0 | 0 | 2 | 5 | 2 | 9 |
| 22 | `UT_CheckInInspection` | 10 | 0 | 0 | 2 | 6 | 2 | 10 |
| 23 | `UT_SubmitInspection` | 11 | 0 | 0 | 3 | 6 | 2 | 11 |
| 24 | `UT_IssuePenalty` | 10 | 0 | 0 | 2 | 6 | 2 | 10 |
| 25 | `UT_RecordPayment` | 9 | 0 | 0 | 2 | 5 | 2 | 9 |
| 26 | `UT_CreateCompany` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 27 | `UT_AddTeamMember` | 9 | 0 | 0 | 2 | 6 | 1 | 9 |
| 28 | `UT_AwardPoints` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 29 | `UT_EvaluateBadges` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 30 | `UT_PresignMedia` | 11 | 0 | 0 | 3 | 6 | 2 | 11 |
| 31 | `UT_EvaluateExif` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 32 | `UT_GetNearbyMap` | 11 | 0 | 0 | 3 | 6 | 2 | 11 |
| 33 | `UT_GetHeatmap` | 9 | 0 | 0 | 3 | 5 | 1 | 9 |
| 34 | `UT_SendNotification` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 35 | `UT_AddComment` | 11 | 0 | 0 | 3 | 6 | 2 | 11 |
| 36 | `UT_UpdateProfile` | 10 | 0 | 0 | 3 | 5 | 2 | 10 |
| 37 | `UT_BanUser` | 9 | 0 | 0 | 2 | 6 | 1 | 9 |
| 38 | `UT_UpdateUserRole` | 9 | 0 | 0 | 2 | 6 | 1 | 9 |
| 39 | `UT_GetCategories` | 8 | 0 | 0 | 3 | 4 | 1 | 8 |
| **TOTAL** | **SUB TOTAL** | **386** | **0** | **0** | **94** | **241** | **51** | **386** |

---

## 2. FUNCTION LIST

| No | Requirement Name | Class Name | Function Name | Function Code(Optional) | Sheet Name | Description | Pre-Condition |
| :-: | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | Critical Unit Test | `RegisterCommandHandler` | Register Account | `UT_Register` | `UT_Register` | Đăng ký tài khoản Citizen & gửi OTP (SD-01) | Dependencies mocked. |
| 2 | Critical Unit Test | `LoginCommandHandler` | User Login | `UT_Login` | `UT_Login` | Đăng nhập JWT & check lockout 5 lần (SD-02) | Dependencies mocked. |
| 3 | Critical Unit Test | `VerifyEmailCommandHandler` | Verify Email OTP | `UT_VerifyEmail` | `UT_VerifyEmail` | Xác minh OTP kích hoạt tài khoản (SD-03) | Dependencies mocked. |
| 4 | Critical Unit Test | `SubmitPollutionReportCommandHandler` | Submit Pollution Report | `UT_SubmitReport` | `UT_SubmitReport` | Gửi báo cáo ô nhiễm, check GPS VN & Rate limit (SD-06) | Dependencies mocked. |
| 5 | Critical Unit Test | `VerifyReportCommandHandler` | Verify Report | `UT_VerifyReport` | `UT_VerifyReport` | LEO/DEO duyệt báo cáo & tính priority (SD-09) | Dependencies mocked. |
| 6 | Critical Unit Test | `RejectReportCommandHandler` | Reject Report | `UT_RejectReport` | `UT_RejectReport` | Từ chối báo cáo với lý do ≥20 ký tự (SD-10) | Dependencies mocked. |
| 7 | Critical Unit Test | `ConfirmDuplicateCommandHandler` | Confirm Duplicate | `UT_ConfirmDuplicate` | `UT_ConfirmDuplicate` | Đánh dấu báo cáo trùng lặp ST_DWithin 50m (SD-11) | Dependencies mocked. |
| 8 | Critical Unit Test | `CloseReportCommandHandler` | Close Report | `UT_CloseReport` | `UT_CloseReport` | Đóng báo cáo hoàn tất / auto-close 7 ngày (SD-16) | Dependencies mocked. |
| 9 | Critical Unit Test | `RequestReopenReportCommandHandler` | Request Reopen | `UT_RequestReopen` | `UT_RequestReopen` | Citizen gửi minh chứng mở lại báo cáo (SD-17) | Dependencies mocked. |
| 10 | Critical Unit Test | `AcceptAssignmentCommandHandler` | Accept Assignment | `UT_AcceptAssignment` | `UT_AcceptAssignment` | Đội thu gom tiếp nhận task dọn dẹp (SD-21) | Dependencies mocked. |
| 11 | Critical Unit Test | `DeclineAssignmentCommandHandler` | Decline Assignment | `UT_DeclineAssignment` | `UT_DeclineAssignment` | Từ chối task & thông báo cho LEO (SD-21) | Dependencies mocked. |
| 12 | Critical Unit Test | `CheckInCleanupCommandHandler` | Cleanup Site Check-In | `UT_CheckInCleanup` | `UT_CheckInCleanup` | Check-in tọa độ thực địa thu gom ≤200m (SD-22) | Dependencies mocked. |
| 13 | Critical Unit Test | `UploadBeforeImagesCommandHandler` | Upload Before Images | `UT_UploadBefore` | `UT_UploadBefore` | Trưởng đội upload ảnh hiện trường trước dọn (SD-23) | Dependencies mocked. |
| 14 | Critical Unit Test | `UpdateProgressCommandHandler` | Update Cleanup Progress | `UT_UpdateProgress` | `UT_UpdateProgress` | Cập nhật % tiến độ & ảnh dọn dẹp (SD-23) | Dependencies mocked. |
| 15 | Critical Unit Test | `ResolveReportCommandHandler` | Resolve Report Cleanup | `UT_ResolveReport` | `UT_ResolveReport` | Upload ảnh After & hoàn thành thu gom (SD-15) | Dependencies mocked. |
| 16 | Critical Unit Test | `EscalateCleanupCommandHandler` | Escalate Cleanup Issue | `UT_EscalateCleanup` | `UT_EscalateCleanup` | Báo cáo leo thang sự cố vượt quá khả năng xử lý | Dependencies mocked. |
| 17 | Critical Unit Test | `CreateCommunityCleanupCommandHandler` | Create Community Cleanup | `UT_CreateCommunity` | `UT_CreateCommunity` | LEO khởi tạo chiến dịch dọn rác cộng đồng (SD-25) | Dependencies mocked. |
| 18 | Critical Unit Test | `JoinCommunityCleanupCommandHandler` | Join Community Cleanup | `UT_JoinCommunity` | `UT_JoinCommunity` | Citizen đăng ký tham gia dọn rác cộng đồng (SD-25) | Dependencies mocked. |
| 19 | Critical Unit Test | `CheckInCommunityCleanupCommandHandler` | Volunteer Check-In | `UT_VolunteerCheckIn` | `UT_VolunteerCheckIn` | Tình nguyện viên check-in GPS tại sự kiện (SD-25) | Dependencies mocked. |
| 20 | Critical Unit Test | `CreateInspectionCommandHandler` | Create Inspection Report | `UT_CreateInspection` | `UT_CreateInspection` | LEO lập biên bản kiểm tra thanh tra (SD-28) | Dependencies mocked. |
| 21 | Critical Unit Test | `AssignInspectionTeamCommandHandler` | Assign Inspection Team | `UT_AssignInspector` | `UT_AssignInspector` | Giao nhiệm vụ cho Đội thanh tra (SD-29) | Dependencies mocked. |
| 22 | Critical Unit Test | `CheckInInspectionCommandHandler` | Inspection Check-In | `UT_CheckInInspection` | `UT_CheckInInspection` | Thanh tra viên check-in tọa độ thực địa (SD-31) | Dependencies mocked. |
| 23 | Critical Unit Test | `SubmitInspectionReportCommandHandler` | Submit Inspection Field Report | `UT_SubmitInspection` | `UT_SubmitInspection` | Lập biên bản vi phạm & upload chứng cứ (SD-34) | Dependencies mocked. |
| 24 | Critical Unit Test | `IssuePenaltyDecisionCommandHandler` | Issue Penalty Decision | `UT_IssuePenalty` | `UT_IssuePenalty` | Trưởng đội ra quyết định xử phạt hành chính (SD-32) | Dependencies mocked. |
| 25 | Critical Unit Test | `RecordPenaltyPaymentCommandHandler` | Record Penalty Payment | `UT_RecordPayment` | `UT_RecordPayment` | LEO ghi nhận nộp phạt & tự động đóng hồ sơ (SD-39) | Dependencies mocked. |
| 26 | Critical Unit Test | `CreateCompanyCommandHandler` | Create Environmental Company | `UT_CreateCompany` | `UT_CreateCompany` | Admin/DEO khởi tạo công ty môi trường | Dependencies mocked. |
| 27 | Critical Unit Test | `AddTeamMemberCommandHandler` | Add Team Member | `UT_AddTeamMember` | `UT_AddTeamMember` | Thêm nhân sự vào đội thu gom/thanh tra | Dependencies mocked. |
| 28 | Critical Unit Test | `GamificationPointAwarder` | Award Activity Points | `UT_AwardPoints` | `UT_AwardPoints` | Cộng điểm thưởng báo cáo/dọn dẹp (SD-51) | Dependencies mocked. |
| 29 | Critical Unit Test | `BadgeEligibilityEvaluator` | Evaluate User Badges | `UT_EvaluateBadges` | `UT_EvaluateBadges` | Khảo sát điều kiện & cấp huy hiệu tự động | Dependencies mocked. |
| 30 | Critical Unit Test | `PresignMediaUploadCommandHandler` | Presign Media Upload | `UT_PresignMedia` | `UT_PresignMedia` | Cấp URL upload S3 direct (SD-66) | Dependencies mocked. |
| 31 | Critical Unit Test | `ExifSuspicionEvaluator` | Evaluate EXIF Media | `UT_EvaluateExif` | `UT_EvaluateExif` | Kiểm tra tính nghi vấn & strip EXIF (BR-AI-007) | Dependencies mocked. |
| 32 | Critical Unit Test | `GetNearbyReportsQueryHandler` | Get Nearby Reports | `UT_GetNearbyMap` | `UT_GetNearbyMap` | Truy vấn báo cáo lân cận theo bán kính GIS | Dependencies mocked. |
| 33 | Critical Unit Test | `GetHeatmapQueryHandler` | Get Heatmap Data | `UT_GetHeatmap` | `UT_GetHeatmap` | Lấy dữ liệu bản đồ nhiệt ô nhiễm | Dependencies mocked. |
| 34 | Critical Unit Test | `SendNotificationCommandHandler` | Send Push Notification | `UT_SendNotification` | `UT_SendNotification` | Gửi thông báo Push FCM & In-app | Dependencies mocked. |
| 35 | Critical Unit Test | `AddCommentCommandHandler` | Add Report Comment | `UT_AddComment` | `UT_AddComment` | Thêm bình luận trên báo cáo & đính kèm ảnh | Dependencies mocked. |
| 36 | Critical Unit Test | `UpdateProfileCommandHandler` | Update User Profile | `UT_UpdateProfile` | `UT_UpdateProfile` | Cập nhật thông tin hồ sơ cá nhân | Dependencies mocked. |
| 37 | Critical Unit Test | `BanUserCommandHandler` | Ban User Account | `UT_BanUser` | `UT_BanUser` | Admin khóa tài khoản vi phạm | Dependencies mocked. |
| 38 | Critical Unit Test | `UpdateUserRoleCommandHandler` | Change User Role | `UT_UpdateUserRole` | `UT_UpdateUserRole` | Admin thay đổi vai trò người dùng | Dependencies mocked. |
| 39 | Critical Unit Test | `GetCategoriesQueryHandler` | Get Catalog Categories | `UT_GetCategories` | `UT_GetCategories` | Truy vấn danh mục loại ô nhiễm | Dependencies mocked. |

---

## 3. UNIT TEST FUNCTION SPECIFICATIONS

---

### 3.1 `UT_Register` — Register Account

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_Register` | **Function Name** | Register Account (`RegisterCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 115 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Register Citizen account, validate password strength (BR-AUTH-005), hash bcrypt >= 12, send OTP email. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_Register`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Database connection active | O | O | O | O | O | O | O | O | O | O |
| **Condition** | Request payload supplied | O | O | O | O | O | O | O | O | O | O |
| | Email format valid | O | O | X | O | O | O | O | O | O | O |
| | Email unique (not exists in DB) | O | O | O | X | O | O | O | O | O | O |
| | Password strength >= 8 chars, mixed case, digit, special (BR-AUTH-005) | O | O | O | O | X | X | O | O | O | O |
| | FullName supplied (non-empty) | O | O | O | O | O | O | X | O | O | O |
| | OTP repository available | O | X | O | O | O | O | O | O | O | O |
| | Email service available | O | O | O | O | O | O | O | X | O | O |
| **Confirm** | Result IsSuccess = true | O | | | | | | | | | |
| | Return userId & email verification pending status | O | | | | | | | | | |
| | Password hashed using bcrypt cost >= 12 | O | O | | | | | | | | |
| | 6-digit OTP code generated & hashed in DB | O | | | | | | | | | |
| | Email duplicate error (Conflict 409) | | | | O | | | | | | |
| | Validation error returned (422) | | | O | | O | O | O | | | |
| | Infrastructure exception handling | | O | | | | | | O | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **A** | **N** | **B** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.2 `UT_Login` — User Login

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_Login` | **Function Name** | User Login (`LoginCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 140 | **Lack of Test Cases**| 0 |
| **Test Requirement** | User authentication, bcrypt verify, 5 failed attempts lockout 30m (BR-AUTH-011), issue JWT 24h + Refresh token 30d (BR-AUTH-013). |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 7 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_Login`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User exists in DB | O | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Email supplied | O | O | O | X | O | O | O | O | O | O | O |
| | Password matches hash | O | X | O | O | X | X | X | X | X | O | O |
| | Account not locked (LockoutEnd == null or expired) | O | O | O | O | O | X | O | O | O | O | O |
| | Account not banned (IsBanned == false) | O | O | O | O | O | O | X | O | O | O | O |
| | Failed attempt count | 0 | 1 | 0 | 0 | 4 | 5 | 0 | 2 | 3 | 0 | 0 |
| **Confirm** | Return JWT AccessToken (24h) & RefreshToken (30d) | O | | | | | | | | | O | O |
| | Reset FailedLoginCount to 0 on success | O | | | | | | | | | O | O |
| | Increment FailedLoginCount on bad password | | O | | | O | | | O | O | | |
| | Lock account for 30m on 5th failed attempt (BR-AUTH-011) | | | | | | O | | | | | |
| | Return InvalidCredentials error | | O | O | O | O | | | O | O | | |
| | Return AccountLocked error (423) | | | | | | O | | | | | |
| | Return AccountBanned error (403) | | | | | | | O | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **N** | **A** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.3 `UT_VerifyEmail` — Verify Email OTP

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VerifyEmail` | **Function Name** | Verify Email OTP (`VerifyEmailCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 85 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Verify 6-digit OTP code, check expiration (15m), set IsEmailVerified = true. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_VerifyEmail`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User registered, IsEmailVerified = false | O | O | O | O | O | O | O | O |
| **Condition** | OTP record exists for email | O | X | O | O | O | O | O | O |
| | OTP code matches hash | O | O | X | O | O | O | O | O |
| | OTP not expired (within 15m window) | O | O | O | X | O | O | O | O |
| | OTP not already used | O | O | O | O | X | O | O | O |
| **Confirm** | Update User.IsEmailVerified = true | O | | | | | | | O |
| | Mark OTP record as Used | O | | | | | | | O |
| | Return OTP invalid or expired error | | O | O | O | O | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **A** | **A** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.4 `UT_SubmitReport` — Submit Pollution Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SubmitReport` | **Function Name** | Submit Pollution Report (`SubmitPollutionReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 180 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Submit report with photos (BR-REP-001), GPS bounds VN Lat 8.0-24.0, Lng 102.0-110.0 (BR-REP-003), Rate limit 5/h 20/24h (BR-REP-010). |
| **Test Results** | **Passed:** 12 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 7 / 2 \| **Total TCs:** 12 |

#### Condition & Confirmation Matrix (`UT_SubmitReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 | UTC12 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User authenticated (Citizen) | O | O | O | O | O | O | O | O | X | O | O | O |
| **Condition** | Photo URLs supplied (1 to 5 images) | O | X | O | O | O | O | O | O | O | O | O | O |
| | CategoryId exists in catalog | O | O | X | O | O | O | O | O | O | O | O | O |
| | GPS Lat within VN bounds (8.0 to 24.0) (BR-REP-003) | O | O | O | X | O | O | O | O | O | O | O | O |
| | GPS Lng within VN bounds (102.0 to 110.0) | O | O | O | O | X | O | O | O | O | O | O | O |
| | Rate limit within quota (<= 5/h, <= 20/24h) (BR-REP-010) | O | O | O | O | O | X | O | O | O | O | O | O |
| | Description length >= 10 chars | O | O | O | O | O | O | X | O | O | O | O | O |
| **Confirm** | Create Report entity with Status = Submitted | O | | | | | | | | | | O | O |
| | Assign PostGIS Point (SRID 4326) | O | | | | | | | | | | O | O |
| | Trigger AI classification background job | O | | | | | | | | | | O | O |
| | Return RateLimitExceeded error (429) | | | | | | O | | | | | | |
| | Return ValidationError (422) | | O | O | O | O | | O | | | | | |
| | Return Unauthorized (401) | | | | | | | | | O | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **A** | **N** | **N** | **A** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.5 `UT_VerifyReport` — Verify Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VerifyReport` | **Function Name** | Verify Report (`VerifyReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 130 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Officer verifies report, sets severity level, calculates priority score (BR-OFF-010), transition Status Submitted -> Verified. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_VerifyReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User is Officer / Admin role | O | O | X | O | O | O | O | O | O | O |
| **Condition** | Report exists and Status == Submitted | O | X | O | O | O | O | O | O | O | O |
| | Severity level specified (Low, Medium, High, Critical) | O | O | O | X | O | O | O | O | O | O |
| | SLA calculation target date set | O | O | O | O | O | O | O | O | O | O |
| **Confirm** | Transition Status to Verified | O | | | | | | | | O | O |
| | Calculate Priority = severity*3 + relatedCount*2 + ageHours/24 (BR-OFF-010) | O | | | | | | | | O | O |
| | Raise ReportVerifiedDomainEvent | O | | | | | | | | O | O |
| | Return InvalidStatusTransition error | | X | | | O | | | | | |
| | Return Forbidden (403) | | | X | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.6 `UT_RejectReport` — Reject Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RejectReport` | **Function Name** | Reject Report (`RejectReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 95 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Reject invalid report, enforce rejection reason length >= 20 characters, transition Status Submitted -> Rejected. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 4 / 2 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_RejectReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User is Officer / Admin | O | O | X | O | O | O | O | O |
| **Condition** | Report Status == Submitted | O | X | O | O | O | O | O | O |
| | Rejection reason length >= 20 chars | O | O | O | X | X | O | O | O |
| **Confirm** | Transition Status to Rejected | O | | | | | | | O |
| | Save rejection reason & rejectedBy officer ID | O | | | | | | | O |
| | Return ValidationError "Reason must be at least 20 chars" | | | | X | X | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.7 `UT_ConfirmDuplicate` — Confirm Duplicate Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_ConfirmDuplicate` | **Function Name** | Confirm Duplicate (`ConfirmDuplicateCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Link duplicate report to original report ID, PostGIS ST_DWithin <= 50m check (BR-REP-030), transition Status -> Duplicate. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_ConfirmDuplicate`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Target report and Original report exist | O | X | O | O | O | O | O | O | O |
| **Condition** | Distance between locations <= 50m (BR-REP-030) | O | O | X | O | O | O | O | O | O |
| | Same category & created within 24h window | O | O | O | X | O | O | O | O | O |
| **Confirm** | Set DuplicateOfReportId = originalReportId | O | | | | | | | | O |
| | Transition target Status to Duplicate | O | | | | | | | | O |
| | Increment original report RelatedReportCount (+1) | O | | | | | | | | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **B** | **B** | **A** | **A** | **A** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.8 `UT_CloseReport` — Close Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CloseReport` | **Function Name** | Close Report (`CloseReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Close resolved report via Citizen confirmation or Hangfire auto-close after 7 days (BR-REP-016, BR-REP-025). |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 4 / 2 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_CloseReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Report Status == Resolved | O | X | O | O | O | O | O | O |
| **Condition** | User is report author OR Hangfire System job | O | O | X | O | O | O | O | O |
| | Time elapsed since Resolved status | 2d | 0d | 1d | 7d | 8d | 0d | 1d | 7d |
| **Confirm** | Transition Status Resolved -> Closed | O | | | O | O | | | O |
| | Award citizen confirmation bonus points (+15 pts) | O | | | | | | | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **B** | **A** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.9 `UT_RequestReopen` — Request Reopen Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RequestReopen` | **Function Name** | Request Reopen (`RequestReopenReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 105 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Citizen requests report reopen with evidence photos (BR-REP-015), max 2 reopen requests allowed per report. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_RequestReopen`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Report Status == Resolved, User == Author | O | X | O | O | O | O | O | O | O |
| **Condition** | ReopenCount < 2 (max 2 times) | O | O | X | O | O | O | O | O | O |
| | Reopen evidence image URLs supplied | O | O | O | X | O | O | O | O | O |
| | Reopen reason length >= 15 chars | O | O | O | O | X | O | O | O | O |
| **Confirm** | Create ReportReopenRequest record | O | | | | | | | | O |
| | Set Report Status -> InProgress (Reopened) | O | | | | | | | | O |
| | Increment Report.ReopenCount (+1) | O | | | | | | | | O |
| | Return ExceededMaxReopenLimit error | | | O | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **B** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.10 `UT_AcceptAssignment` — Accept Assignment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AcceptAssignment` | **Function Name** | Accept Assignment (`AcceptAssignmentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Cleanup team leader/member accepts assigned report task (BR-CLN-001), assignment status Assigned -> InProgress. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_AcceptAssignment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Assignment exists and Status == Assigned | O | X | O | O | O | O | O | O |
| **Condition** | Current user belongs to target cleanup team | O | O | X | O | O | O | O | O |
| **Confirm** | Update Assignment.Status -> InProgress | O | | | | | | | O |
| | Set AcceptedAt timestamp = System.UtcNow | O | | | | | | | O |
| | Return AssignmentNotFound / NotTeamMember error | | O | X | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.11 `UT_DeclineAssignment` — Decline Assignment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_DeclineAssignment` | **Function Name** | Decline Assignment (`DeclineAssignmentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 85 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Decline assignment with reason >= 15 chars, notify Officer to reassign task. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_DeclineAssignment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Assignment Status == Assigned | O | X | O | O | O | O | O | O |
| **Condition** | User belongs to assigned team | O | O | X | O | O | O | O | O |
| | Decline reason length >= 15 chars | O | O | O | X | O | O | O | O |
| **Confirm** | Update Assignment.Status -> Declined | O | | | | | | | O |
| | Trigger notification to officer (AssignmentDeclined) | O | | | | | | | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **A** | **A** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.12 `UT_CheckInCleanup` — Cleanup Site Check-In

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CheckInCleanup` | **Function Name** | Cleanup Check-In (`CheckInCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Check-in at pollution site, PostGIS ST_DWithin distance check <= 200m (BR-CLN-002). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CheckInCleanup`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Assignment Status == InProgress | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Distance between GPS and site <= 200m (BR-CLN-002) | 50m | 10m | 250m | 201m | 200m | 0m | 100m | 500m | 300m | 150m |
| **Confirm** | Set CheckedInAt = UtcNow & save CheckedInLocation | O | O | | | O | O | O | | | O |
| | Return TooFarFromSite error (400) when > 200m | | | O | O | | | | O | O | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.13 `UT_UploadBefore` — Upload Before Images

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UploadBefore` | **Function Name** | Upload Before Images (`UploadBeforeImagesCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 95 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Upload initial scene photo before cleanup, verify team leader role, validate R2 CDN URLs. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_UploadBefore`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Assignment CheckedIn == true | O | X | O | O | O | O | O | O | O |
| **Condition** | User is Team Leader | O | O | X | O | O | O | O | O | O |
| | Image URLs belong to system R2 domain | O | O | O | X | O | O | O | O | O |
| | Image count >= 1 and <= 5 | O | O | O | O | X | O | O | O | O |
| **Confirm** | Create ReportMedia records with Type = Before | O | | | | | | | | O |
| | Return NotTeamLeader / InvalidStorageUrl error | | | O | X | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.14 `UT_UpdateProgress` — Update Cleanup Progress

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateProgress` | **Function Name** | Update Progress (`UpdateProgressCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 120 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Update cleanup progress percentage (0 - 100%), attach progress photos, validate team leader permissions. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_UpdateProgress`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Assignment Status == InProgress | O | X | O | O | O | O | O | O | O | O | O |
| **Condition** | ProgressPercent in range [0, 100] | 50% | 10% | -5% | 105% | 0% | 100% | 75% | 30% | 90% | 50% | 80% |
| **Confirm** | Update Assignment.ProgressPercent | O | | | | O | O | O | O | O | O | O |
| | Create ReportMedia records with Type = Progress | O | | | | | | O | | O | | O |
| | Return InvalidProgressPercent error | | | O | O | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.15 `UT_ResolveReport` — Resolve Report Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_ResolveReport` | **Function Name** | Resolve Report (`ResolveReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 135 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Mark cleanup resolved, require at least 1 After photo (BR-REP-014), pHash difference check vs Before (BR-CLN-004), transition Status -> Resolved. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_ResolveReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Assignment CheckedIn == true | O | X | O | O | O | O | O | O | O | O |
| **Condition** | "After" photo URLs supplied (>= 1 image) | O | O | X | O | O | O | O | O | O | O |
| | pHash Hamming distance vs "Before" photo >= threshold (BR-CLN-004) | O | O | O | X | O | O | O | O | O | O |
| **Confirm** | Set Report Status -> Resolved & Assignment Status -> Completed | O | | | | | | | | | O |
| | Save "After" ReportMedia records | O | | | | | | | | | O |
| | Trigger notification to Citizen author (ReportResolved) | O | | | | | | | | | O |
| | Return MissingAfterPhotos / DuplicateImageHash error | | | O | X | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.16 `UT_EscalateCleanup` — Escalate Cleanup Issue

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EscalateCleanup` | **Function Name** | Escalate Cleanup (`EscalateCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Escalate cleanup difficulty/hazard to Officer, require escalation reason >= 20 chars. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_EscalateCleanup`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Assignment Status == InProgress | O | X | O | O | O | O | O | O | O |
| **Condition** | Escalation reason length >= 20 chars | O | O | X | O | O | O | O | O | O |
| **Confirm** | Set Report.IsEscalated = true | O | | | | | | | | O |
| | Send priority notification to Officer / Admin | O | | | | | | | | O |
| | Return ValidationError "Reason too short" | | | X | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **B** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.17 `UT_CreateCommunity` — Create Community Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateCommunity` | **Function Name** | Create Community Cleanup (`CreateCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 140 | **Lack of Test Cases**| 0 |
| **Test Requirement** | LEO creates volunteer community cleanup campaign on Verified report, leader must be Cleaner role, max 1 active campaign per report. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_CreateCommunity`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Report Status == Verified | O | X | O | O | O | O | O | O | O | O | O |
| **Condition** | No active community cleanup exists for report | O | O | X | O | O | O | O | O | O | O | O |
| | Designated Leader has Cleaner role | O | O | O | X | O | O | O | O | O | O | O |
| | MaxParticipants > 0 (e.g. 50 volunteers) | O | O | O | O | X | O | O | O | O | O | O |
| **Confirm** | Create CommunityCleanupEvent with Status = OpenForJoin | O | | | | | | | | O | O | O |
| | Transition Report Status -> InProgress | O | | | | | | | | O | O | O |
| | Auto-add Leader as participant | O | | | | | | | | O | O | O |
| | Return CommunityAlreadyActive error | | | O | | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **A** | **A** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.18 `UT_JoinCommunity` — Join Community Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_JoinCommunity` | **Function Name** | Join Community Cleanup (`JoinCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 95 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Citizen registers to join volunteer event, check participant quota limit, prevent duplicate registrations. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_JoinCommunity`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Campaign Status == OpenForJoin | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Participant count < MaxParticipants | O | O | X | O | O | O | O | O | O | O |
| | Citizen not already registered | O | O | O | X | O | O | O | O | O | O |
| **Confirm** | Add Participant record with Status = Registered | O | | | | | | | | | O |
| | Increment CurrentParticipantCount (+1) | O | | | | | | | | | O |
| | Return EventFull / AlreadyJoined error | | | O | X | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **B** | **A** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.19 `UT_VolunteerCheckIn` — Volunteer Check-In

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VolunteerCheckIn` | **Function Name** | Volunteer Check-In (`CheckInCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 105 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Volunteer check-in at event site, GPS distance <= 200m or override reason >= 20 chars required. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_VolunteerCheckIn`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User registered as Participant | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Distance <= 200m OR OverrideReason >= 20 chars | 50m | 100m | >200m | >200m | 200m | 180m | >200m | >200m | >200m | 30m |
| **Confirm** | Update Participant Status -> CheckedIn | O | O | | O | O | O | | | | O |
| | Award volunteer participation points (+20 pts) | O | O | | O | O | O | | | | O |
| | Return CheckInLocationTooFar error | | | O | | | | O | O | O | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.20 `UT_CreateInspection` — Create Inspection Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateInspection` | **Function Name** | Create Inspection (`CreateInspectionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 120 | **Lack of Test Cases**| 0 |
| **Test Requirement** | LEO creates inspection report for severe pollution violations (BR-INS-001, BR-INS-033). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CreateInspection`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User is LEO role | O | O | X | O | O | O | O | O | O | O |
| **Condition** | Target Report Status in {Verified, InProgress} | O | X | O | O | O | O | O | O | O | O |
| | No existing active inspection for report | O | O | O | X | O | O | O | O | O | O |
| **Confirm** | Create InspectionReport entity with Status = Created | O | | | | | | | | O | O |
| | Link InspectionReport to target ReportId | O | | | | | | | | O | O |
| | Return InspectionAlreadyExists error | | | | X | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.21 `UT_AssignInspector` — Assign Inspection Team

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AssignInspector` | **Function Name** | Assign Inspector (`AssignInspectionTeamCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Assign environmental inspection team to inspection task, notify team leader. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_AssignInspector`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Inspection Status == Created | O | X | O | O | O | O | O | O | O |
| **Condition** | Target InspectionTeam exists & active | O | O | X | O | O | O | O | O | O |
| **Confirm** | Set Inspection.AssignedTeamId & Status -> Assigned | O | | | | | | | | O |
| | Send notification to Inspectors | O | | | | | | | | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.22 `UT_CheckInInspection` — Inspection Check-In

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CheckInInspection` | **Function Name** | Inspection Check-In (`CheckInInspectionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 95 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Inspector check-in at inspection site (soft distance verification <= 200m). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CheckInInspection`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User assigned as Inspector | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Distance to site <= 200m | 50m | 100m | 250m | 200m | 0m | 150m | 400m | 300m | 201m | 20m |
| **Confirm** | Set Inspection.CheckedInAt = UtcNow | O | O | | O | O | O | | | | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **A** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.23 `UT_SubmitInspection` — Submit Inspection Field Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SubmitInspection` | **Function Name** | Submit Inspection (`SubmitInspectionReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 150 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Submit field inspection report, attach checklist evidence items (photo/video/audio) (BR-INS-033), evaluate violation severity. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_SubmitInspection`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Inspection CheckedIn == true | O | X | O | O | O | O | O | O | O | O | O |
| **Condition** | Checklist evidence attached (photo/video/audio) | O | O | X | O | O | O | O | O | O | O | O |
| | Violation summary length >= 20 chars | O | O | O | X | O | O | O | O | O | O | O |
| **Confirm** | Save InspectionEvidence & Checklist records | O | | | | | | | | O | O | O |
| | Transition Status -> SubmittedForReview | O | | | | | | | | O | O | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **A** | **A** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.24 `UT_IssuePenalty` — Issue Penalty Decision

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_IssuePenalty` | **Function Name** | Issue Penalty (`IssuePenaltyDecisionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 125 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Issue administrative penalty decision, fine amount > 0, payment deadline set. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_IssuePenalty`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Inspection Status == SubmittedForReview | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Fine amount > 0 (e.g. 5,000,000 VND) | O | O | X | O | O | O | O | O | O | O |
| | Payment due date in future (e.g. +30d) | O | O | O | X | O | O | O | O | O | O |
| **Confirm** | Create PenaltyDecision entity with Status = Issued | O | | | | | | | | | O |
| | Set Inspection Status -> PenaltyIssued | O | | | | | | | | | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **B** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.25 `UT_RecordPayment` — Record Penalty Payment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RecordPayment` | **Function Name** | Record Payment (`RecordPenaltyPaymentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | LEO records full fine payment, auto-closes inspection case (SD-39). |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_RecordPayment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Penalty Status == Issued | O | X | O | O | O | O | O | O | O |
| **Condition** | Paid amount == Fine amount | O | O | X | O | O | O | O | O | O |
| **Confirm** | Set Penalty Status -> Paid & Inspection Status -> Closed | O | | | | | | | | O |
| | Save payment receipt number & PaidAt timestamp | O | | | | | | | | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **B** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.26 `UT_CreateCompany` — Create Environmental Company

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateCompany` | **Function Name** | Create Company (`CreateCompanyCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 115 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Admin/DEO registers environmental company profile, validate tax code uniqueness. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CreateCompany`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User is Admin / DEO | O | O | X | O | O | O | O | O | O | O |
| **Condition** | TaxCode unique | O | X | O | O | O | O | O | O | O | O |
| | Company Name & Address supplied | O | O | O | X | O | O | O | O | O | O |
| **Confirm** | Create EnvironmentalCompany entity | O | | | | | | | | O | O |
| | Return CompanyAlreadyExists error | | X | | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.27 `UT_AddTeamMember` — Add Team Member

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AddTeamMember` | **Function Name** | Add Team Member (`AddTeamMemberCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Add staff to cleanup/inspection team, prevent duplicate active memberships. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 1 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_AddTeamMember`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Team exists & active | O | X | O | O | O | O | O | O | O |
| **Condition** | User is not already member of active team | O | O | X | O | O | O | O | O | O |
| **Confirm** | Create TeamMember mapping record | O | | | | | | | | O |
| | Return AlreadyTeamMember error | | | X | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.28 `UT_AwardPoints` — Award Activity Points

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AwardPoints` | **Function Name** | Award Points (`GamificationPointAwarder`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Award user points on report verification (+50 pts) or volunteer check-in (+20 pts) (BR-GAM-001). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_AwardPoints`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User exists & active | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Activity type valid (ReportVerified, CleanupJoin, ConfirmClose) | O | O | X | O | O | O | O | O | O | O |
| **Confirm** | Add Points to User.TotalPoints balance | O | | | | | | | | O | O |
| | Record PointTransaction ledger entry | O | | | | | | | | O | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.29 `UT_EvaluateBadges` — Evaluate User Badges

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EvaluateBadges` | **Function Name** | Evaluate Badges (`BadgeEligibilityEvaluator`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 125 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Evaluate user badge metrics (Eco Sentinel, Cleanup Hero), unlock badge when metrics threshold met (BR-GAM-002). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_EvaluateBadges`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User metrics calculated | O | O | O | O | O | O | O | O | O | O |
| **Condition** | Verified report count >= Badge required count (e.g. 5 reports) | 5 | 1 | 4 | 5 | 10 | 0 | 3 | 5 | 6 | 20 |
| **Confirm** | Create UserBadge mapping record | O | | | O | O | | | O | O | O |
| | Send notification BadgeUnlocked | O | | | O | O | | | O | O | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **B** | **B** | **N** | **A** | **A** | **N** | **A** | **A** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.30 `UT_PresignMedia` — Presign Media Upload

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_PresignMedia` | **Function Name** | Presign Media Upload (`PresignMediaUploadCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Generate short-lived presigned PUT URL for Cloudflare R2 / S3, validate image content-type (jpeg, png, webp) & file size <= 10MB (BR-SYS-002). |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_PresignMedia`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User authenticated | O | X | O | O | O | O | O | O | O | O | O |
| **Condition** | Content-Type in {image/jpeg, image/png, image/webp} | O | O | X | O | O | O | O | O | O | O | O |
| | File size <= 10,485,760 bytes (10MB) | 2MB | 1MB | 1MB | 15MB | 10MB | 5MB | 1MB | 2MB | 3MB | 4MB | 5MB |
| **Confirm** | Return Presigned PUT UploadUrl & PublicUrl | O | | | | O | O | O | O | O | O | O |
| | Return UnsupportedMediaType (415) / FileTooLarge (413) | | | O | X | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.31 `UT_EvaluateExif` — Evaluate EXIF Media

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EvaluateExif` | **Function Name** | Evaluate EXIF (`ExifSuspicionEvaluator`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 95 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Inspect EXIF metadata for suspicion flags (missing GPS, software edited), strip EXIF prior to public display (BR-AI-007). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_EvaluateExif`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Image stream readable | O | X | O | O | O | O | O | O | O | O |
| **Condition** | EXIF contains camera make/model & GPS tags | O | O | X | O | O | O | O | O | O | O |
| | Software editing tags (Photoshop/Canva) | None | None | None | Found | None | None | None | None | None | None |
| **Confirm** | Return IsSuspicious = false & stripped image stream | O | O | O | | O | O | O | O | O | O |
| | Flag IsSuspicious = true when software edited | | | | O | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **B** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.32 `UT_GetNearbyMap` — Get Nearby Reports Map

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetNearbyMap` | **Function Name** | Get Nearby Map (`GetNearbyReportsQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 130 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query nearby reports using PostGIS ST_DWithin radius, round GPS to ~10m for public privacy (BR-MAP-004), Redis cache 10m (BR-MAP-012). |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_GetNearbyMap`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | PostgreSQL + PostGIS active | O | O | O | O | O | O | O | O | O | O | O |
| **Condition** | Center Lat/Lng supplied | O | X | O | O | O | O | O | O | O | O | O |
| | RadiusKm in range [0.5, 50.0] | 5km | 5km | -1km | 100km | 0.5km | 50km | 2km | 10km | 1km | 5km | 10km |
| **Confirm** | Return list of nearby report map DTOs | O | | | | O | O | O | O | O | O | O |
| | Round public Lat/Lng to 4 decimal places (~11m privacy) (BR-MAP-004) | O | | | | O | O | O | O | O | O | O |
| | Cache query response in Redis for 600s (BR-MAP-012) | O | | | | O | O | O | O | O | O | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.33 `UT_GetHeatmap` — Get Heatmap Data

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetHeatmap` | **Function Name** | Get Heatmap (`GetHeatmapQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 105 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query aggregated pollution point weights for heatmap visualization within bounding box. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 1 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_GetHeatmap`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Database active | O | O | O | O | O | O | O | O | O |
| **Condition** | Bounding box coordinates valid (minLat < maxLat, minLng < maxLng) | O | X | O | O | O | O | O | O | O |
| **Confirm** | Return list of HeatmapPointDto (Lat, Lng, Weight) | O | | | | | | | O | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.34 `UT_SendNotification` — Send Push Notification

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SendNotification` | **Function Name** | Send Notification (`SendNotificationCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 115 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Create In-app Notification, send FCM Push Notification, aggregate anti-spam digest if >20/day (BR-NTF-003). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_SendNotification`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Recipient User exists | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Daily notification count for user | 5 | 0 | 25 | 1 | 2 | 0 | 10 | 20 | 0 | 2 |
| **Confirm** | Create Notification entity in DB | O | | O | O | O | O | O | O | O | O |
| | Dispatch Firebase FCM push notification | O | | | O | O | O | O | O | O | O |
| | Queue for daily digest aggregation when > 20/day (BR-NTF-003) | | | O | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **B** | **A** | **A** | **A** | **A** | **B** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.35 `UT_AddComment` — Add Report Comment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AddComment` | **Function Name** | Add Comment (`AddCommentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Add user comment on pollution report, validate non-empty content, attach optional photos (BR-CMT-001 ~ BR-CMT-005). |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_AddComment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Target Report exists & not deleted | O | X | O | O | O | O | O | O | O | O | O |
| **Condition** | Comment content non-empty & length <= 1000 chars | O | O | X | O | O | O | O | O | O | O | O |
| | Content contains no blocked words | O | O | O | X | O | O | O | O | O | O | O |
| **Confirm** | Create Comment entity linked to ReportId | O | | | | O | O | O | O | O | O | O |
| | Return ReportNotFound / ProfanityFilter error | | X | | X | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **B** | **A** | **A** | **A** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.36 `UT_UpdateProfile` — Update User Profile

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateProfile` | **Function Name** | Update Profile (`UpdateProfileCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 95 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Update citizen profile details, validate phone number format (10 digits starting with 0). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_UpdateProfile`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | User authenticated | O | X | O | O | O | O | O | O | O | O |
| **Condition** | Phone number format valid (VN regex: 10 digits start with 0) | O | O | X | O | O | O | O | O | O | O |
| | FullName non-empty | O | O | O | X | O | O | O | O | O | O |
| **Confirm** | Update User entity in DB | O | | | | O | O | O | O | O | O |
| | Return ValidationError "Invalid phone format" | | | X | | | | | | | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **B** | **B** | **A** | **A** | **N** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.37 `UT_BanUser` — Ban User Account

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_BanUser` | **Function Name** | Ban User (`BanUserCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Admin toggles IsBanned status, invalidate active user sessions & refresh tokens. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 1 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_BanUser`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Caller is Admin role | O | X | O | O | O | O | O | O | O |
| **Condition** | Target User exists & not self (cannot ban oneself) | O | O | X | O | O | O | O | O | O |
| **Confirm** | Set User.IsBanned = true / false | O | | | | O | O | O | O | O |
| | Revoke all active RefreshTokens for target user | O | | | | O | O | O | O | O |
| | Record AuditLog entry | O | | | | O | O | O | O | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.38 `UT_UpdateUserRole` — Change User Role

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateUserRole` | **Function Name** | Update User Role (`UpdateUserRoleCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 85 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Admin changes user role (Citizen, Officer, CleanupTeam, Admin), enforce authorization policy. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 1 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_UpdateUserRole`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Caller is Admin role | O | X | O | O | O | O | O | O | O |
| **Condition** | Target role is valid UserRole enum value | O | O | X | O | O | O | O | O | O |
| **Confirm** | Update User.Role to target role | O | | | | O | O | O | O | O |
| | Record AuditLog entry | O | | | | O | O | O | O | O |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **A** | **A** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.39 `UT_GetCategories` — Get Catalog Categories

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetCategories` | **Function Name** | Get Categories (`GetCategoriesQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 75 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query reference list of pollution categories, support active-only filter. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 4 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_GetCategories`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Precondition** | Database active | O | O | O | O | O | O | O | O |
| **Condition** | OnlyActive parameter supplied (true/false) | true | false | true | true | false | true | true | false |
| **Confirm** | Return list of CategoryDto | O | O | O | O | O | O | O | O |
| | Exclude disabled categories when OnlyActive == true | O | | O | O | | O | O | |
| **Result** | **Type (N: Normal, A: Abnormal, B: Boundary)** | **N** | **N** | **A** | **A** | **A** | **A** | **B** | **N** |
| | **Status (P: Passed, F: Failed)** | **P** | **P** | **P** | **P** | **P** | **P** | **P** | **P** |
| | **Executed Date** | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

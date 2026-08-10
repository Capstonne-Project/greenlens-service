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
| **Total Test Cases** | **377** |
| **Passed (P)** | **377 (100.00%)** |
| **Failed (F)** | **0 (0.00%)** |
| **Untested** | **0 (0.00%)** |
| **Normal Cases (N)** | **95 (25.20%)** |
| **Abnormal Cases (A)** | **212 (56.23%)** |
| **Boundary Cases (B)** | **70 (18.57%)** |
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
| **TOTAL** | **SUB TOTAL** | **377** | **0** | **0** | **95** | **212** | **70** | **377** |

---

## 2. FUNCTION LIST

| No | Requirement Name | Class Name | Function Name | Function Code(Optional) | Sheet Name | Description | Pre-Condition |
| :-: | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | Critical Unit Test | `RegisterCommandHandler` | Register Account | `UT_Register` | `UT_Register` | Register Citizen account and send verification OTP | Dependencies mocked. |
| 2 | Critical Unit Test | `LoginCommandHandler` | User Login | `UT_Login` | `UT_Login` | User login with JWT authentication and 5-attempt lockout | Dependencies mocked. |
| 3 | Critical Unit Test | `VerifyEmailCommandHandler` | Verify Email OTP | `UT_VerifyEmail` | `UT_VerifyEmail` | Verify email OTP code for account activation | Dependencies mocked. |
| 4 | Critical Unit Test | `SubmitPollutionReportCommandHandler` | Submit Pollution Report | `UT_SubmitReport` | `UT_SubmitReport` | Submit pollution report with photos, Vietnam GPS check and rate limit | Dependencies mocked. |
| 5 | Critical Unit Test | `VerifyReportCommandHandler` | Verify Report | `UT_VerifyReport` | `UT_VerifyReport` | Officer verifies report and calculates priority score | Dependencies mocked. |
| 6 | Critical Unit Test | `RejectReportCommandHandler` | Reject Report | `UT_RejectReport` | `UT_RejectReport` | Reject invalid report with reason minimum 20 characters | Dependencies mocked. |
| 7 | Critical Unit Test | `ConfirmDuplicateCommandHandler` | Confirm Duplicate | `UT_ConfirmDuplicate` | `UT_ConfirmDuplicate` | Mark duplicate report linked to original report within 50m | Dependencies mocked. |
| 8 | Critical Unit Test | `CloseReportCommandHandler` | Close Report | `UT_CloseReport` | `UT_CloseReport` | Close resolved report after confirmation or auto-close 7 days | Dependencies mocked. |
| 9 | Critical Unit Test | `RequestReopenReportCommandHandler` | Request Reopen | `UT_RequestReopen` | `UT_RequestReopen` | Citizen requests report reopen with evidence photos | Dependencies mocked. |
| 10 | Critical Unit Test | `AcceptAssignmentCommandHandler` | Accept Assignment | `UT_AcceptAssignment` | `UT_AcceptAssignment` | Cleanup team accepts assigned cleanup task | Dependencies mocked. |
| 11 | Critical Unit Test | `DeclineAssignmentCommandHandler` | Decline Assignment | `UT_DeclineAssignment` | `UT_DeclineAssignment` | Cleanup team declines task with reason and notifies officer | Dependencies mocked. |
| 12 | Critical Unit Test | `CheckInCleanupCommandHandler` | Cleanup Site Check-In | `UT_CheckInCleanup` | `UT_CheckInCleanup` | Check-in at cleanup site within 200m distance limit | Dependencies mocked. |
| 13 | Critical Unit Test | `UploadBeforeImagesCommandHandler` | Upload Before Images | `UT_UploadBefore` | `UT_UploadBefore` | Team leader uploads scene photos before cleanup | Dependencies mocked. |
| 14 | Critical Unit Test | `UpdateProgressCommandHandler` | Update Cleanup Progress | `UT_UpdateProgress` | `UT_UpdateProgress` | Update cleanup progress percentage and progress photos | Dependencies mocked. |
| 15 | Critical Unit Test | `ResolveReportCommandHandler` | Resolve Report Cleanup | `UT_ResolveReport` | `UT_ResolveReport` | Upload After photos and mark cleanup resolved | Dependencies mocked. |
| 16 | Critical Unit Test | `EscalateCleanupCommandHandler` | Escalate Cleanup Issue | `UT_EscalateCleanup` | `UT_EscalateCleanup` | Escalate cleanup issue exceeding team capacity | Dependencies mocked. |
| 17 | Critical Unit Test | `CreateCommunityCleanupCommandHandler` | Create Community Cleanup | `UT_CreateCommunity` | `UT_CreateCommunity` | LEO creates volunteer community cleanup campaign | Dependencies mocked. |
| 18 | Critical Unit Test | `JoinCommunityCleanupCommandHandler` | Join Community Cleanup | `UT_JoinCommunity` | `UT_JoinCommunity` | Citizen registers to join community cleanup campaign | Dependencies mocked. |
| 19 | Critical Unit Test | `CheckInCommunityCleanupCommandHandler` | Volunteer Check-In | `UT_VolunteerCheckIn` | `UT_VolunteerCheckIn` | Volunteer checks in at event site with GPS verification | Dependencies mocked. |
| 20 | Critical Unit Test | `CreateInspectionCommandHandler` | Create Inspection Report | `UT_CreateInspection` | `UT_CreateInspection` | LEO creates environmental inspection report | Dependencies mocked. |
| 21 | Critical Unit Test | `AssignInspectionTeamCommandHandler` | Assign Inspection Team | `UT_AssignInspector` | `UT_AssignInspector` | Assign inspection task to environmental inspection team | Dependencies mocked. |
| 22 | Critical Unit Test | `CheckInInspectionCommandHandler` | Inspection Check-In | `UT_CheckInInspection` | `UT_CheckInInspection` | Inspector checks in at site with GPS verification | Dependencies mocked. |
| 23 | Critical Unit Test | `SubmitInspectionReportCommandHandler` | Submit Inspection Field Report | `UT_SubmitInspection` | `UT_SubmitInspection` | Submit field inspection report and evidence checklist | Dependencies mocked. |
| 24 | Critical Unit Test | `IssuePenaltyDecisionCommandHandler` | Issue Penalty Decision | `UT_IssuePenalty` | `UT_IssuePenalty` | Issue administrative penalty decision with fine amount | Dependencies mocked. |
| 25 | Critical Unit Test | `RecordPenaltyPaymentCommandHandler` | Record Penalty Payment | `UT_RecordPayment` | `UT_RecordPayment` | LEO records penalty fine payment and auto-closes inspection | Dependencies mocked. |
| 26 | Critical Unit Test | `CreateCompanyCommandHandler` | Create Environmental Company | `UT_CreateCompany` | `UT_CreateCompany` | Admin/DEO registers environmental company profile | Dependencies mocked. |
| 27 | Critical Unit Test | `AddTeamMemberCommandHandler` | Add Team Member | `UT_AddTeamMember` | `UT_AddTeamMember` | Add staff member to cleanup or inspection team | Dependencies mocked. |
| 28 | Critical Unit Test | `GamificationPointAwarder` | Award Activity Points | `UT_AwardPoints` | `UT_AwardPoints` | Award activity points for verified report or volunteer cleanup | Dependencies mocked. |
| 29 | Critical Unit Test | `BadgeEligibilityEvaluator` | Evaluate User Badges | `UT_EvaluateBadges` | `UT_EvaluateBadges` | Evaluate user metrics and auto-unlock badges | Dependencies mocked. |
| 30 | Critical Unit Test | `PresignMediaUploadCommandHandler` | Presign Media Upload | `UT_PresignMedia` | `UT_PresignMedia` | Generate presigned URL for direct S3 upload | Dependencies mocked. |
| 31 | Critical Unit Test | `ExifSuspicionEvaluator` | Evaluate EXIF Media | `UT_EvaluateExif` | `UT_EvaluateExif` | Evaluate EXIF metadata suspicion and strip EXIF | Dependencies mocked. |
| 32 | Critical Unit Test | `GetNearbyReportsQueryHandler` | Get Nearby Reports | `UT_GetNearbyMap` | `UT_GetNearbyMap` | Query nearby reports using PostGIS radius and round GPS coordinates | Dependencies mocked. |
| 33 | Critical Unit Test | `GetHeatmapQueryHandler` | Get Heatmap Data | `UT_GetHeatmap` | `UT_GetHeatmap` | Query aggregated pollution point weights for heatmap | Dependencies mocked. |
| 34 | Critical Unit Test | `SendNotificationCommandHandler` | Send Push Notification | `UT_SendNotification` | `UT_SendNotification` | Send Push FCM and In-app notification | Dependencies mocked. |
| 35 | Critical Unit Test | `AddCommentCommandHandler` | Add Report Comment | `UT_AddComment` | `UT_AddComment` | Add comment on report with optional photo attachment | Dependencies mocked. |
| 36 | Critical Unit Test | `UpdateProfileCommandHandler` | Update User Profile | `UT_UpdateProfile` | `UT_UpdateProfile` | Update user personal profile details | Dependencies mocked. |
| 37 | Critical Unit Test | `BanUserCommandHandler` | Ban User Account | `UT_BanUser` | `UT_BanUser` | Admin bans or unbans user account | Dependencies mocked. |
| 38 | Critical Unit Test | `UpdateUserRoleCommandHandler` | Change User Role | `UT_UpdateUserRole` | `UT_UpdateUserRole` | Admin updates user role | Dependencies mocked. |
| 39 | Critical Unit Test | `GetCategoriesQueryHandler` | Get Catalog Categories | `UT_GetCategories` | `UT_GetCategories` | Query catalog reference list of pollution categories | Dependencies mocked. |

---

## 3. UNIT TEST FUNCTION SPECIFICATIONS

---

### 3.1 `UT_Register` — Register Account

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_Register` | **Function Name** | Register Account (`RegisterCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Register Citizen account, validate password strength, hash bcrypt >= 12, send OTP email. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_Register`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | Database connection active & OTP repo ready | O | O | O | O | O | O | O | O | O | O |
| | Request payload supplied | O | O | O | O | O | O | O | O | O | O |
| | Email format valid | O | O |   | O | O | O | O | O | O | O |
| | Email unique (not exists in DB) | O | O | O |   | O | O | O | O | O | O |
| | Password strength >= 8 chars, mixed case, digit, special | O | O | O | O |   |   | O | O | O | O |
| | FullName supplied (non-empty) | O | O | O | O | O | O |   | O | O | O |
| | Email service available | O | O | O | O | O | O | O |   | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | O | | | O | O |
| | Controlled failure | | O | O | O | O | | O | O | | |
| | Not found | | | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | | O | O | | |
| | State updated | O | | | | | O | | | O | O |
| | Exception / fallback | | O | | | | | | O | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | A | N | B |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.2 `UT_Login` — User Login

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_Login` | **Function Name** | User Login (`LoginCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | User authentication, bcrypt verify, 5 failed attempts lockout 30m, issue JWT 24h + Refresh token 30d. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 7 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_Login`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | |
| | User exists in database | O | O |   | O | O | O | O | O | O | O | O |
| | Email supplied | O | O | O |   | O | O | O | O | O | O | O |
| | Password matches hash | O |   | O | O |   |   |   |   |   | O | O |
| | Account not locked (LockoutEnd == null or expired) | O | O | O | O | O |   | O | O | O | O | O |
| | Account not banned (IsBanned == false) | O | O | O | O | O | O |   | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;Failed attempt count within limit (< 5) | O | O | O | O | O |   | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | |
| | Success | O | | | | | | | | | O | O |
| | Controlled failure | | O | O | O | O | O | O | O | O | | |
| | Not found | | | O | | | | | | | | |
| | Quota / limit | | | | | | O | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | O | | |
| | State updated | O | O | | | O | O | | O | O | O | O |
| | Exception / fallback | | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | B | A | A | A | N | A |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.3 `UT_VerifyEmail` — Verify Email OTP

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VerifyEmail` | **Function Name** | Verify Email OTP (`VerifyEmailCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Verify 6-digit OTP code, check expiration (15m), set IsEmailVerified = true. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_VerifyEmail`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | |
| | User registered & IsEmailVerified = false | O | O | O | O | O | O | O | O |
| | OTP record exists for email | O |   | O | O | O | O | O | O |
| | OTP code matches hash | O | O |   | O | O | O | O | O |
| | OTP not expired (within 15m window) | O | O | O |   | O | O | O | O |
| | OTP not already used | O | O | O | O |   | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | |
| | Success | O | | | | | | | O |
| | Controlled failure | | O | O | O | O | O | O | |
| | Not found | | O | | | | | | |
| | Quota / limit | | | | O | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | |
| | State updated | O | | | | | | | O |
| | Exception / fallback | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | A | A | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.4 `UT_SubmitReport` — Submit Pollution Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SubmitReport` | **Function Name** | Submit Pollution Report (`SubmitPollutionReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 120 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Submit report with photos, GPS bounds VN Lat 8.0-24.0, Lng 102.0-110.0, Rate limit 5/h 20/24h. |
| **Test Results** | **Passed:** 12 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 7 / 2 \| **Total TCs:** 12 |

#### Condition & Confirmation Matrix (`UT_SubmitReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 | UTC12 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | | |
| | User authenticated (Citizen) | O | O | O | O | O | O | O | O |   | O | O | O |
| | Photo URLs supplied (1 to 5 images) | O |   | O | O | O | O | O | O | O | O | O | O |
| | CategoryId exists in catalog | O | O |   | O | O | O | O | O | O | O | O | O |
| | GPS Lat within VN bounds (8.0 to 24.0) | O | O | O |   | O | O | O | O | O | O | O | O |
| | GPS Lng within VN bounds (102.0 to 110.0) | O | O | O | O |   | O | O | O | O | O | O | O |
| | Rate limit within quota (<= 5/h, <= 20/24h) | O | O | O | O | O |   | O | O | O | O | O | O |
| | Description length >= 10 chars | O | O | O | O | O | O |   | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | | |
| | Success | O | | | | | | | | | O | O | |
| | Controlled failure | | O | O | O | O | O | O | O | O | | | O |
| | Not found | | | O | | | | | | | | | |
| | Quota / limit | | | | | | O | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | O | | | O |
| | State updated | O | | | | | | | | | O | O | |
| | Exception / fallback | | | | | | | | | | | | O |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | B | A | A | A | A | N | N | A |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.5 `UT_VerifyReport` — Verify Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VerifyReport` | **Function Name** | Verify Report (`VerifyReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Officer verifies report, sets severity level, calculates priority score, transition Status Submitted -> Verified. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_VerifyReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User is Officer / Admin role | O | O |   | O | O | O | O | O | O | O |
| | Report exists and Status == Submitted | O |   | O | O | O | O | O | O | O | O |
| | Severity level specified (Low, Medium, High, Critical) | O | O | O |   | O | O | O | O | O | O |
| | SLA calculation target date set | O | O | O | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | O | | O | O | O |
| | Controlled failure | | O | O | O | O | | O | | | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | | O | | | |
| | State updated | O | | | | | O | | O | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.6 `UT_RejectReport` — Reject Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RejectReport` | **Function Name** | Reject Report (`RejectReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Reject invalid report, enforce rejection reason length >= 20 characters, transition Status Submitted -> Rejected. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 4 / 2 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_RejectReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | |
| | User is Officer / Admin | O | O |   | O | O | O | O | O |
| | Report Status == Submitted | O |   | O | O | O | O | O | O |
| | Rejection reason length >= 20 chars | O | O | O |   |   | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | |
| | Success | O | | | | | | O | O |
| | Controlled failure | | O | O | O | O | O | | |
| | Not found | | O | | | | | | |
| | Quota / limit | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | | |
| | State updated | O | | | | | | O | O |
| | Exception / fallback | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.7 `UT_ConfirmDuplicate` — Confirm Duplicate Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_ConfirmDuplicate` | **Function Name** | Confirm Duplicate (`ConfirmDuplicateCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Link duplicate report to original report ID, PostGIS ST_DWithin <= 50m check, transition Status -> Duplicate. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_ConfirmDuplicate`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Target report and Original report exist | O |   | O | O | O | O | O | O | O |
| | Distance between locations <= 50m | O | O |   | O | O | O | O | O | O |
| | Same category & created within 24h window | O | O | O |   | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | | | | | | O |
| | Controlled failure | | O | O | O | O | O | O | O | |
| | Not found | | O | | | | | | | |
| | Quota / limit | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | |
| | State updated | O | | | | | | | | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | B | B | A | A | A | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.8 `UT_CloseReport` — Close Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CloseReport` | **Function Name** | Close Report (`CloseReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Close resolved report via Citizen confirmation or Hangfire auto-close after 7 days. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 4 / 2 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_CloseReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | |
| | Report Status == Resolved | O |   | O | O | O | O | O | O |
| | User is report author OR Hangfire System job | O | O |   | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;Time elapsed >= 7 days auto-close threshold |   |   |   | O | O |   |   | O |
| **Confirm** | Expected outcome | | | | | | | | |
| | Success | O | | | O | O | | | O |
| | Controlled failure | | O | O | | | O | O | |
| | Not found | | O | | | | | | |
| | Quota / limit | | | | | | | | |
| | Rollback / no side effect | | O | O | | | O | O | |
| | State updated | O | | | O | O | | | O |
| | Exception / fallback | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | B | A | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.9 `UT_RequestReopen` — Request Reopen Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RequestReopen` | **Function Name** | Request Reopen (`RequestReopenReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Citizen requests report reopen with evidence photos, max 2 reopen requests allowed per report. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_RequestReopen`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Report Status == Resolved, User == Author | O |   | O | O | O | O | O | O | O |
| | ReopenCount < 2 (max 2 times) | O | O |   | O | O | O | O | O | O |
| | Reopen evidence image URLs supplied | O | O | O |   | O | O | O | O | O |
| | Reopen reason length >= 15 chars | O | O | O | O |   | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | | | | | O | O |
| | Controlled failure | | O | O | O | O | O | O | | |
| | Not found | | O | | | | | | | |
| | Quota / limit | | | O | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | | |
| | State updated | O | | | | | | | O | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | B | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.10 `UT_AcceptAssignment` — Accept Assignment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AcceptAssignment` | **Function Name** | Accept Assignment (`AcceptAssignmentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Cleanup team leader/member accepts assigned report task, assignment status Assigned -> InProgress. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_AcceptAssignment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | |
| | Assignment exists & Status == Assigned | O |   | O | O | O | O | O | O |
| | Current user belongs to target cleanup team | O | O |   | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | |
| | Success | O | | | | | O | | O |
| | Controlled failure | | O | O | O | O | | O | |
| | Not found | | O | | | | | | |
| | Quota / limit | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | | O | |
| | State updated | O | | | | | O | | O |
| | Exception / fallback | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.11 `UT_DeclineAssignment` — Decline Assignment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_DeclineAssignment` | **Function Name** | Decline Assignment (`DeclineAssignmentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Decline assignment with reason >= 15 chars, notify Officer to reassign task. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_DeclineAssignment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | |
| | Assignment Status == Assigned | O |   | O | O | O | O | O | O |
| | User belongs to assigned team | O | O |   | O | O | O | O | O |
| | Decline reason length >= 15 chars | O | O | O |   | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | |
| | Success | O | | | | | | | O |
| | Controlled failure | | O | O | O | O | O | O | |
| | Not found | | O | | | | | | |
| | Quota / limit | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | |
| | State updated | O | | | | | | | O |
| | Exception / fallback | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | A | A | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.12 `UT_CheckInCleanup` — Cleanup Site Check-In

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CheckInCleanup` | **Function Name** | Cleanup Check-In (`CheckInCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Check-in at pollution site, PostGIS ST_DWithin distance check <= 200m. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CheckInCleanup`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | Assignment Status == InProgress | O |   | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;Distance between GPS and site <= 200m | O | O |   |   | O | O | O |   |   | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | O | | | O | O | O | | | O |
| | Controlled failure | | | O | O | | | | O | O | |
| | Not found | | | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | | O | O | | | | O | O | |
| | State updated | O | O | | | O | O | O | | | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | B | A | A | A | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.13 `UT_UploadBefore` — Upload Before Images

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UploadBefore` | **Function Name** | Upload Before Images (`UploadBeforeImagesCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Upload initial scene photo before cleanup, verify team leader role, validate R2 CDN URLs. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_UploadBefore`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Assignment CheckedIn == true | O |   | O | O | O | O | O | O | O |
| | User is Team Leader | O | O |   | O | O | O | O | O | O |
| | Image URLs belong to system R2 domain | O | O | O |   | O | O | O | O | O |
| | Image count >= 1 and <= 5 | O | O | O | O |   | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | | | | | O | O |
| | Controlled failure | | O | O | O | O | O | O | | |
| | Not found | | O | | | | | | | |
| | Quota / limit | | | | | O | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | | |
| | State updated | O | | | | | | | O | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.14 `UT_UpdateProgress` — Update Cleanup Progress

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateProgress` | **Function Name** | Update Progress (`UpdateProgressCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Update cleanup progress percentage (0 - 100%), attach progress photos, validate team leader permissions. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_UpdateProgress`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | |
| | Assignment Status == InProgress | O |   | O | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;ProgressPercent in range [0, 100] | O | O |   |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | |
| | Success | O | O | | | O | O | O | O | O | O | O |
| | Controlled failure | | | O | O | | | | | | | |
| | Not found | |   | | | | | | | | | |
| | Quota / limit | | | | | | | | | | | |
| | Rollback / no side effect | | | O | O | | | | | | | |
| | State updated | O | O | | | O | O | O | O | O | O | O |
| | Exception / fallback | | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | B | A | A | A | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.15 `UT_ResolveReport` — Resolve Report Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_ResolveReport` | **Function Name** | Resolve Report (`ResolveReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Mark cleanup resolved, require at least 1 After photo, pHash difference check vs Before, transition Status -> Resolved. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_ResolveReport`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | Assignment CheckedIn == true | O |   | O | O | O | O | O | O | O | O |
| | "After" photo URLs supplied (>= 1 image) | O | O |   | O | O | O | O | O | O | O |
| | pHash Hamming distance vs "Before" photo >= threshold | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | O | | | O | O |
| | Controlled failure | | O | O | O | O | | O | O | | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | | O | O | | |
| | State updated | O | | | | | O | | | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.16 `UT_EscalateCleanup` — Escalate Cleanup Issue

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EscalateCleanup` | **Function Name** | Escalate Cleanup (`EscalateCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Escalate cleanup difficulty/hazard to Officer, require escalation reason >= 20 chars. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_EscalateCleanup`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Assignment Status == InProgress | O |   | O | O | O | O | O | O | O |
| | Escalation reason length >= 20 chars | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | | | | | O | O |
| | Controlled failure | | O | O | O | O | O | O | | |
| | Not found | | O | | | | | | | |
| | Quota / limit | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | | |
| | State updated | O | | | | | | | O | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | B | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.17 `UT_CreateCommunity` — Create Community Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateCommunity` | **Function Name** | Create Community Cleanup (`CreateCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | LEO creates volunteer community cleanup campaign on Verified report, leader must be Cleaner role, max 1 active campaign per report. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_CreateCommunity`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | |
| | Report Status == Verified | O |   | O | O | O | O | O | O | O | O | O |
| | No active community cleanup exists for report | O | O |   | O | O | O | O | O | O | O | O |
| | Designated Leader has Cleaner role | O | O | O |   | O | O | O | O | O | O | O |
| | MaxParticipants > 0 (e.g. 50 volunteers) | O | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | |
| | Success | O | | | | | | | | O | O | O |
| | Controlled failure | | O | O | O | O | O | O | O | | | |
| | Not found | | O | | | | | | | | | |
| | Quota / limit | | | O | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | | | |
| | State updated | O | | | | | | | | O | O | O |
| | Exception / fallback | | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | A | A | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.18 `UT_JoinCommunity` — Join Community Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_JoinCommunity` | **Function Name** | Join Community Cleanup (`JoinCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Citizen registers to join volunteer event, check participant quota limit, prevent duplicate registrations. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_JoinCommunity`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | Campaign Status == OpenForJoin | O |   | O | O | O | O | O | O | O | O |
| | Participant count < MaxParticipants | O | O |   | O | O | O | O | O | O | O |
| | Citizen not already registered | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | | | | O | O |
| | Controlled failure | | O | O | O | O | O | O | O | | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | O | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | | |
| | State updated | O | | | | | | | | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | B | A | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.19 `UT_VolunteerCheckIn` — Volunteer Check-In

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VolunteerCheckIn` | **Function Name** | Volunteer Check-In (`CheckInCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Volunteer check-in at event site, GPS distance <= 200m or override reason >= 20 chars required. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_VolunteerCheckIn`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User registered as Participant | O |   | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;Distance <= 200m OR OverrideReason >= 20 chars | O | O |   | O | O | O |   |   |   | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | O | | O | O | O | | | | O |
| | Controlled failure | | | O | | | | O | O | O | |
| | Not found | | | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | | O | | | | O | O | O | |
| | State updated | O | O | | O | O | O | | | | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | B | A | A | A | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.20 `UT_CreateInspection` — Create Inspection Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateInspection` | **Function Name** | Create Inspection (`CreateInspectionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | LEO creates inspection report for severe pollution violations. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CreateInspection`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User is LEO role | O | O |   | O | O | O | O | O | O | O |
| | Target Report Status in {Verified, InProgress} | O |   | O | O | O | O | O | O | O | O |
| | No existing active inspection for report | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | O | | O | O | O |
| | Controlled failure | | O | O | O | O | | O | | | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | | O | | | | | | |
| | Rollback / no side effect | | O | O | O | O | | O | | | |
| | State updated | O | | | | | O | | O | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.21 `UT_AssignInspector` — Assign Inspection Team

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AssignInspector` | **Function Name** | Assign Inspector (`AssignInspectionTeamCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Assign environmental inspection team to inspection task, notify team leader. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_AssignInspector`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Inspection Status == Created | O |   | O | O | O | O | O | O | O |
| | Target InspectionTeam exists & active | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | | | | | O | O |
| | Controlled failure | | O | O | O | O | O | O | | |
| | Not found | | O | O | | | | | | |
| | Quota / limit | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | | |
| | State updated | O | | | | | | | O | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.22 `UT_CheckInInspection` — Inspection Check-In

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CheckInInspection` | **Function Name** | Inspection Check-In (`CheckInInspectionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Inspector check-in at inspection site (soft distance verification <= 200m). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CheckInInspection`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User assigned as Inspector | O |   | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;Distance to site <= 200m | O | O |   | O | O | O |   |   |   | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | O | | O | O | O | | | | O |
| | Controlled failure | | | O | | | | O | O | O | |
| | Not found | | | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | | O | | | | O | O | O | |
| | State updated | O | O | | O | O | O | | | | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | B | A | A | A | A | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.23 `UT_SubmitInspection` — Submit Inspection Field Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SubmitInspection` | **Function Name** | Submit Inspection (`SubmitInspectionReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Submit field inspection report, attach checklist evidence items (photo/video/audio), evaluate violation severity. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_SubmitInspection`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | |
| | Inspection CheckedIn == true | O |   | O | O | O | O | O | O | O | O | O |
| | Checklist evidence attached (photo/video/audio) | O | O |   | O | O | O | O | O | O | O | O |
| | Violation summary length >= 20 chars | O | O | O |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | |
| | Success | O | | | | | | | | O | O | O |
| | Controlled failure | | O | O | O | O | O | O | O | | | |
| | Not found | | O | | | | | | | | | |
| | Quota / limit | | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | | | |
| | State updated | O | | | | | | | | O | O | O |
| | Exception / fallback | | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | A | A | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.24 `UT_IssuePenalty` — Issue Penalty Decision

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_IssuePenalty` | **Function Name** | Issue Penalty (`IssuePenaltyDecisionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Issue administrative penalty decision, fine amount > 0, payment deadline set. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_IssuePenalty`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | Inspection Status == SubmittedForReview | O |   | O | O | O | O | O | O | O | O |
| | Fine amount > 0 (e.g. 5,000,000 VND) | O | O |   | O | O | O | O | O | O | O |
| | Payment due date in future (e.g. +30d) | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | | | | | O |
| | Controlled failure | | O | O | O | O | O | O | O | O | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | O | |
| | State updated | O | | | | | | | | | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | B | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.25 `UT_RecordPayment` — Record Penalty Payment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RecordPayment` | **Function Name** | Record Payment (`RecordPenaltyPaymentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | LEO records full fine payment, auto-closes inspection case. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_RecordPayment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Penalty Status == Issued | O |   | O | O | O | O | O | O | O |
| | Paid amount == Fine amount | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | | | | | | O |
| | Controlled failure | | O | O | O | O | O | O | O | |
| | Not found | | O | | | | | | | |
| | Quota / limit | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | |
| | State updated | O | | | | | | | | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | B | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.26 `UT_CreateCompany` — Create Environmental Company

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateCompany` | **Function Name** | Create Company (`CreateCompanyCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Admin/DEO registers environmental company profile, validate tax code uniqueness. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CreateCompany`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User is Admin / DEO | O | O |   | O | O | O | O | O | O | O |
| | TaxCode unique | O |   | O | O | O | O | O | O | O | O |
| | Company Name & Address supplied | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | O | | O | O | O |
| | Controlled failure | | O | O | O | O | | O | | | |
| | Not found | | | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | | O | | | |
| | State updated | O | | | | | O | | O | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

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
| **Condition** | Precondition | | | | | | | | | |
| | Team exists & active | O |   | O | O | O | O | O | O | O |
| | User is not already member of active team | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | | | | | | O |
| | Controlled failure | | O | O | O | O | O | O | O | |
| | Not found | | O | | | | | | | |
| | Quota / limit | | | O | | | | | | |
| | Rollback / no side effect | | O | O | O | O | O | O | O | |
| | State updated | O | | | | | | | | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.28 `UT_AwardPoints` — Award Activity Points

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AwardPoints` | **Function Name** | Award Points (`GamificationPointAwarder`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Award user points on report verification (+50 pts) or volunteer check-in (+20 pts). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_AwardPoints`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User exists & active | O |   | O | O | O | O | O | O | O | O |
| | Activity type valid (ReportVerified, CleanupJoin, ConfirmClose) | O | O |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | | O | | O | O | O |
| | Controlled failure | | O | O | O | O | | O | | | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | O | | O | | | |
| | State updated | O | | | | | O | | O | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.29 `UT_EvaluateBadges` — Evaluate User Badges

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EvaluateBadges` | **Function Name** | Evaluate Badges (`BadgeEligibilityEvaluator`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Evaluate user badge metrics (Eco Sentinel, Cleanup Hero), unlock badge when metrics threshold met. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_EvaluateBadges`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User metrics calculated | O | O | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;Verified report count >= Badge required count (e.g. 5 reports) | O |   |   | O | O |   |   | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | O | O | | | O | O | O |
| | Controlled failure | | O | O | | | O | O | | | |
| | Not found | | | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | | | O | O | | | |
| | State updated | O | | | O | O | | | O | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | B | B | N | A | A | N | A | A |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.30 `UT_PresignMedia` — Presign Media Upload

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_PresignMedia` | **Function Name** | Presign Media Upload (`PresignMediaUploadCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Generate short-lived presigned PUT URL for Cloudflare R2 / S3, validate image content-type (jpeg, png, webp) & file size <= 10MB. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_PresignMedia`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | |
| | User authenticated | O |   | O | O | O | O | O | O | O | O | O |
| | Content-Type in {image/jpeg, image/png, image/webp} | O | O |   | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;File size <= 10,485,760 bytes (10MB) | O | O | O |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | |
| | Success | O | | | | O | O | O | O | O | O | O |
| | Controlled failure | | O | O | O | | | | | | | |
| | Not found | | | | | | | | | | | |
| | Quota / limit | | | | O | | | | | | | |
| | Rollback / no side effect | | O | O | O | | | | | | | |
| | State updated | | | | | | | | | | | |
| | Exception / fallback | | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | B | A | A | A | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.31 `UT_EvaluateExif` — Evaluate EXIF Media

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EvaluateExif` | **Function Name** | Evaluate EXIF (`ExifSuspicionEvaluator`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Inspect EXIF metadata for suspicion flags (missing GPS, software edited), strip EXIF prior to public display. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_EvaluateExif`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | Image stream readable | O |   | O | O | O | O | O | O | O | O |
| | EXIF contains camera make/model & GPS tags | O | O |   | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;No software editing tags found (clean EXIF) | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | O | O | | O | O | O | O | O | O |
| | Controlled failure | | | | O | | | | | | |
| | Not found | | | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | | | O | | | | | | |
| | State updated | | | | | | | | | | |
| | Exception / fallback | | O | | O | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | B | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.32 `UT_GetNearbyMap` — Get Nearby Reports Map

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetNearbyMap` | **Function Name** | Get Nearby Map (`GetNearbyReportsQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query nearby reports using PostGIS ST_DWithin radius, round GPS to ~10m for public privacy, Redis cache 10m. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_GetNearbyMap`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | |
| | PostgreSQL + PostGIS active | O | O | O | O | O | O | O | O | O | O | O |
| | Center Lat/Lng supplied | O |   | O | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;RadiusKm in range [0.5, 50.0] | O | O |   |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | |
| | Success | O | | | | O | O | O | O | O | O | O |
| | Controlled failure | | O | O | O | | | | | | | |
| | Not found | | | | | | | | | | | |
| | Quota / limit | | | | O | | | | | | | |
| | Rollback / no side effect | | O | O | O | | | | | | | |
| | State updated | | | | | | | | | | | |
| | Exception / fallback | | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | B | A | A | A | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.33 `UT_GetHeatmap` — Get Heatmap Data

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetHeatmap` | **Function Name** | Get Heatmap (`GetHeatmapQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query aggregated pollution point weights for heatmap visualization within bounding box. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 1 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_GetHeatmap`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Database active | O | O | O | O | O | O | O | O | O |
| | Bounding box coordinates valid (minLat < maxLat, minLng < maxLng) | O |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | O | O | O | O | O | O | O |
| | Controlled failure | | O | | | | | | | |
| | Not found | | | | | | | | | |
| | Quota / limit | | | | | | | | | |
| | Rollback / no side effect | | O | | | | | | | |
| | State updated | | | | | | | | | |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.34 `UT_SendNotification` — Send Push Notification

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SendNotification` | **Function Name** | Send Notification (`SendNotificationCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Create In-app Notification, send FCM Push Notification, aggregate anti-spam digest if >20/day. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_SendNotification`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | Recipient User exists | O |   | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;Daily notification count <= 20 anti-spam limit | O | O |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | O | O | O | O | O | O | O | O |
| | Controlled failure | | O | | | | | | | | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | O | | | | | | | |
| | Rollback / no side effect | | O | | | | | | | | |
| | State updated | O | | O | O | O | O | O | O | O | O |
| | Exception / fallback | | | O | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | B | A | A | A | A | B | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.35 `UT_AddComment` — Add Report Comment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AddComment` | **Function Name** | Add Comment (`AddCommentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Add user comment on pollution report, validate non-empty content, attach optional photos. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_AddComment`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | | |
| | Target Report exists & not deleted | O |   | O | O | O | O | O | O | O | O | O |
| | Comment content non-empty & length <= 1000 chars | O | O |   | O | O | O | O | O | O | O | O |
| | Content contains no blocked words | O | O | O |   | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | | |
| | Success | O | | | | O | O | O | O | O | O | O |
| | Controlled failure | | O | O | O | | | | | | | |
| | Not found | | O | | | | | | | | | |
| | Quota / limit | | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | | | | | | | |
| | State updated | O | | | | O | O | O | O | O | O | O |
| | Exception / fallback | | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | B | A | A | A | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.36 `UT_UpdateProfile` — Update User Profile

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateProfile` | **Function Name** | Update Profile (`UpdateProfileCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Update citizen profile details, validate phone number format (10 digits starting with 0). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 5 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_UpdateProfile`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | | |
| | User authenticated | O |   | O | O | O | O | O | O | O | O |
| | Phone number format valid (VN regex: 10 digits start with 0) | O | O |   | O | O | O | O | O | O | O |
| | FullName non-empty | O | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | | |
| | Success | O | | | | O | O | O | O | O | O |
| | Controlled failure | | O | O | O | | | | | | |
| | Not found | | O | | | | | | | | |
| | Quota / limit | | | | | | | | | | |
| | Rollback / no side effect | | O | O | O | | | | | | |
| | State updated | O | | | | O | O | O | O | O | O |
| | Exception / fallback | | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | B | B | A | A | N | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

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
| **Condition** | Precondition | | | | | | | | | |
| | Caller is Admin role | O |   | O | O | O | O | O | O | O |
| | Target User exists & not self (cannot ban oneself) | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | O | O | O | O | O | O |
| | Controlled failure | | O | O | | | | | | |
| | Not found | | | O | | | | | | |
| | Quota / limit | | | | | | | | | |
| | Rollback / no side effect | | O | O | | | | | | |
| | State updated | O | | | O | O | O | O | O | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.38 `UT_UpdateUserRole` — Change User Role

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateUserRole` | **Function Name** | Update User Role (`UpdateUserRoleCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Admin changes user role (Citizen, Officer, CleanupTeam, Admin), enforce authorization policy. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 1 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_UpdateUserRole`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | | |
| | Caller is Admin role | O |   | O | O | O | O | O | O | O |
| | Target role is valid UserRole enum value | O | O |   | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | | |
| | Success | O | | | O | O | O | O | O | O |
| | Controlled failure | | O | O | | | | | | |
| | Not found | | | | | | | | | |
| | Quota / limit | | | | | | | | | |
| | Rollback / no side effect | | O | O | | | | | | |
| | State updated | O | | | O | O | O | O | O | O |
| | Exception / fallback | | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | A | A | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

### 3.39 `UT_GetCategories` — Get Catalog Categories

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetCategories` | **Function Name** | Get Categories (`GetCategoriesQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query reference list of pollution categories, support active-only filter. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 4 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_GetCategories`)

| Category | Condition / Confirmation Item | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | Precondition | | | | | | | | |
| | Database active | O | O | O | O | O | O | O | O |
| | &nbsp;&nbsp;&nbsp;&nbsp;OnlyActive parameter supplied | O | O | O | O | O | O | O | O |
| **Confirm** | Expected outcome | | | | | | | | |
| | Success | O | O | O | O | O | O | O | O |
| | Controlled failure | | | | | | | | |
| | Not found | | | | | | | | |
| | Quota / limit | | | | | | | | |
| | Rollback / no side effect | | | | | | | | |
| | State updated | | | | | | | | |
| | Exception / fallback | | | | | | | | |
| **Result** | Type (N: Normal, A: Abnormal, B: Boundary) | N | N | A | A | A | A | B | N |
| | Passed / Failed | P | P | P | P | P | P | P | P |
| | Executed Date | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |

---

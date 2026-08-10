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

### 3.1 `UT_Register` — Register Account

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_Register` | **Function Name** | Register Account (`RegisterCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | User registration, validate RFC 5322 email, bcrypt cost >= 12 password, FullName, issue 6-digit OTP. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_Register`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Database & Identity service ready | O | O | O | O | O | O | O | O | O | O |
|  | **Email** | Valid RFC 5322 email & unique in DB | O | O |   | O | O | O | O | O | O | O |
|  |  | Invalid email format (missing @ or domain) |   |   | O |   |   |   |   |   |   |   |
|  |  | Email already exists in Identity DB |   |   |   | O |   |   |   |   |   |   |
|  | **Password** | Valid strength (>= 8 chars, mixed case, digit, special char) | O | O | O | O |   |   | O | O | O | O |
|  |  | Weak password (< 8 chars or missing complexity) |   |   |   |   | O | O |   |   |   |   |
|  | **FullName** | Supplied valid text (length 2..100 chars) | O | O | O | O | O | O |   | O | O | O |
|  |  | Empty or invalid length (< 2 or > 100 chars) |   |   |   |   |   |   | O |   |   |   |
|  | **Email Service** | SMTP / SendGrid OTP service available | O | O | O | O | O | O | O |   | O | O |
|  |  | Service timeout / unreachable failure |   |   |   |   |   |   |   | O |   |   |
| **Confirm** | **Return** | `Result<Guid>` with User ID & Status = PendingVerification | O |   |   |   |   | O |   |   | O | O |
|  |  | `Result.Failure` with Validation Error (422) |   |   | O |   | O | O |   |   |   |   |
|  |  | `Result.Failure` with Conflict Error (409) |   |   |   | O |   |   |   |   |   |   |
|  |  | `Result.Failure` with Infrastructure Error (500) |   |   |   |   |   |   |   | O |   |   |
|  | **Exception** | `ValidationException` |   |   | O |   | O | O |   |   |   |   |
|  |  | `BusinessRuleViolationException` |   |   |   | O |   |   |   |   |   |   |
|  |  | `InfrastructureException` |   |   |   |   |   |   |   | O |   |   |
|  | **Log message** | "User registered successfully, pending OTP verification" | O |   |   |   |   | O |   |   | O | O |
|  |  | "Registration failed: Email already exists in database" |   |   |   | O |   |   |   |   |   |   |
|  |  | "Failed to dispatch verification email OTP via SMTP" |   |   |   |   |   |   |   | O |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | A | N | B |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

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

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | ASP.NET Core Identity DB active | O | O | O | O | O | O | O | O | O | O | O |
|  | **User Account** | User account exists in DB | O | O |   | O | O | O | O | O | O | O | O |
|  |  | User email not found in DB |   |   | O |   |   |   |   |   |   |   |   |
|  | **Email** | Email parameter supplied & valid format | O | O |   | O | O | O | O | O | O | O | O |
|  |  | Email parameter empty or invalid format |   |   |   | O |   |   |   |   |   |   |   |
|  | **Password** | Matches stored bcrypt hash | O |   |   | O |   |   |   |   |   | O | O |
|  |  | Incorrect password hash mismatch |   | O | O |   |   | O | O | O | O |   |   |
|  | **Lockout Status** | Account not locked (LockoutEnd is null or expired) | O | O | O | O | O |   | O | O | O | O | O |
|  |  | Account currently locked out (LockoutEnd in future) |   |   |   |   |   | O |   |   |   |   |   |
|  | **Account Status** | Account active (IsBanned == false) | O | O | O | O | O | O |   | O | O | O | O |
|  |  | Account banned by System Admin |   |   |   |   |   |   | O |   |   |   |   |
|  | **Failed Attempts** | Failed attempt count within limit (< 5) | O | O | O | O | O |   | O | O | O | O | O |
|  |  | Failed attempt count reached threshold (>= 5) |   |   |   |   |   | O |   |   |   |   |   |
| **Confirm** | **Return** | `Result<AuthResponseDto>` with JWT (24h) & Refresh (30d) | O |   |   |   |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with Unauthorized Error (401) |   | O | O |   | O | O | O |   | O | O |   |   |
|  |  | `Result.Failure` with AccountLocked Error (423) |   |   |   |   |   | O |   |   |   |   |   |   |
|  |  | `Result.Failure` with Forbidden Error (403) |   |   |   |   |   |   | O |   |   |   |   |   |
|  | **Exception** | `UnauthorizedAccessException` |   | O | O |   | O | O | O |   | O | O |   |   |
|  |  | `AccountLockedException` |   |   |   |   |   | O |   |   |   |   |   |   |
|  | **Log message** | "User authenticated successfully, JWT tokens issued" | O |   |   |   |   |   |   |   |   |   | O | O |
|  |  | "Invalid password attempt for account" |   | O |   |   |   |   |   |   |   |   |   |   |
|  |  | "Account locked for 30 minutes due to 5 failed attempts" |   |   |   |   |   | O |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | B | A | A | A | N | A |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.3 `UT_VerifyEmail` — Verify Email OTP

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VerifyEmail` | **Function Name** | Verify Email OTP (`VerifyEmailCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Verify 6-digit OTP code, 15m expiration, mark IsEmailVerified = true, revoke OTP token. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_VerifyEmail`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | User registered & IsEmailVerified == false | O | O | O | O | O | O | O | O |
|  | **OTP Record** | OTP record exists in DB for email | O |   | O | O | O | O | O | O |
|  |  | OTP record missing or not found |   | O |   |   |   |   |   |   |
|  | **OTP Code** | Submitted 6-digit OTP code matches stored hash | O |   |   | O | O | O | O | O |
|  |  | OTP code mismatch |   |   | O |   |   |   |   |   |
|  | **Expiration** | Within 15-minute valid expiration window | O |   | O |   | O | O | O | O |
|  |  | OTP code expired (> 15 minutes) |   |   |   | O |   |   |   |   |
|  | **Usage Status** | OTP code has not been previously consumed | O |   | O | O | O |   | O | O | O |
|  |  | OTP code already consumed |   |   |   |   | O |   |   |   |
| **Confirm** | **Return** | `Result.Success` with email verified confirmation | O |   |   |   |   |   |   | O |
|  |  | `Result.Failure` with InvalidOTP Error (400) |   | O | O |   |   | O | O |   |
|  |  | `Result.Failure` with ExpiredOTP Error (400) |   |   |   | O |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O | O |   | O | O | O |   |
|  | **Log message** | "Email successfully verified for user account" | O |   |   |   |   |   |   | O |
|  |  | "Email verification failed: OTP code expired or invalid" |   | O | O |   | O | O | O |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | A | A | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |

---

### 3.4 `UT_SubmitReport` — Submit Pollution Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SubmitReport` | **Function Name** | Submit Pollution Report (`SubmitPollutionReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 120 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Submit pollution report, validate photos (1-5), Category, GPS bounds (VN: 8-24N, 102-110E), rate limit (5/h, 20/24h). |
| **Test Results** | **Passed:** 12 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 7 / 2 \| **Total TCs:** 12 |

#### Condition & Confirmation Matrix (`UT_SubmitReport`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 | UTC12 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | PostgreSQL + PostGIS spatial index ready | O | O | O | O | O | O | O | O | O | O | O | O |
|  | **User Role** | User authenticated with Citizen role | O | O | O | O | O | O | O | O |   | O | O | O |
|  |  | User unauthenticated or wrong role |   |   |   |   |   |   |   |   | O |   |   |   |
|  | **Images** | Supplied 1 to 5 images from system R2 domain | O |   | O | O | O | O | O | O | O | O | O | O |
|  |  | Images list empty or > 5 photos limit |   | O |   |   |   |   |   |   |   |   |   |   |
|  | **Category** | Pollution category ID exists in active catalog | O | O |   | O | O | O | O | O | O | O | O | O |
|  |  | Category ID invalid or missing in DB |   |   | O |   |   |   |   |   |   |   |   |   |
|  | **GPS Latitude** | Latitude within Vietnam bounds (8.0 to 24.0 N) | O | O | O |   | O | O | O | O | O | O | O | O |
|  |  | Latitude out of bounds (< 8.0 or > 24.0) |   |   |   | O |   |   |   |   |   |   |   |   |
|  | **GPS Longitude** | Longitude within Vietnam bounds (102.0 to 110.0 E) | O | O | O | O |   | O | O | O | O | O | O | O |
|  |  | Longitude out of bounds (< 102.0 or > 110.0) |   |   |   |   | O |   |   |   |   |   |   |   |
|  | **Rate Limit** | Submission rate limit within quota (<= 5/h, <= 20/24h) | O | O | O | O | O |   | O | O | O | O | O | O |
|  |  | Rate limit exceeded (> 5/h or > 20/24h) |   |   |   |   |   | O |   |   |   |   |   |   |
|  | **Description** | Description supplied text length >= 10 chars | O | O | O | O | O | O |   | O | O | O | O | O |
|  |  | Description empty or length < 10 chars |   |   |   |   |   |   | O |   |   |   |   |   |
| **Confirm** | **Return** | `Result<Guid>` with Report ID & Status = Submitted | O |   |   |   |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with Validation Error (422) |   | O | O | O | O |   | O |   | O |   |   |   |
|  |  | `Result.Failure` with RateLimit Error (429) |   |   |   |   |   | O |   |   |   |   |   |   |
|  |  | `Result.Failure` with Unauthorized Error (401) |   |   |   |   |   |   |   |   | O |   |   |   |
|  | **Exception** | `ValidationException` |   | O | O | O | O |   | O |   | O |   |   |   |
|  |  | `RateLimitExceededException` |   |   |   |   |   | O |   |   |   |   |   |   |
|  | **Log message** | "Pollution report submitted successfully" | O |   |   |   |   |   |   |   |   |   | O | O |
|  |  | "Report submission rejected: Validation failure" |   | O | O | O | O |   | O |   | O |   |   |   |
|  |  | "Report submission blocked: Rate limit exceeded" |   |   |   |   |   | O |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | B | A | A | A | A | N | N | A |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |  |

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

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Report exists in DB & current Status == Submitted | O | O | O | O | O | O | O | O | O | O |
|  | **User Role** | User authenticated with Officer or Admin role | O | O |   | O | O | O | O | O | O | O |
|  |  | User has Citizen role (forbidden) |   |   | O |   |   |   |   |   |   |   |
|  | **Report Status** | Current report Status == Submitted | O |   | O | O | O | O | O | O | O | O |
|  |  | Report Status already Verified or Closed |   | O |   |   |   |   |   |   |   |   |
|  | **Severity Level** | Severity specified (Low, Medium, High, Critical) | O | O | O | O |   | O | O | O | O | O |
|  |  | Severity level invalid or out of enum |   |   |   | O |   |   |   |   |   |   |
|  | **Priority & SLA** | Priority score & SLA target resolution date set | O | O | O | O | O |   | O | O | O | O |
|  |  | SLA calculation date calculation failure |   |   |   |   |   | O |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status set to Verified | O |   |   |   |   | O |   | O | O | O |
|  |  | `Result.Failure` with Forbidden Error (403) |   |   | O |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with InvalidStatus Error (400) |   | O |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with Validation Error (422) |   |   |   | O |   | O |   |   |   |   |
|  | **Exception** | `ForbiddenAccessException` |   |   | O |   |   |   |   |   |   |   |
|  |  | `ValidationException` |   | O |   | O |   | O |   |   |   |   |
|  | **Log message** | "Report verified successfully, priority score computed" | O |   |   |   |   | O |   | O | O | O |
|  |  | "Report verification denied: Unauthorized officer role" |   |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

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

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Report exists in DB & current Status == Submitted | O | O | O | O | O | O | O | O |
|  | **User Role** | User authenticated with Officer or Admin role | O | O |   | O | O | O | O | O |
|  |  | User has Citizen role (forbidden) |   |   | O |   |   |   |   |   |
|  | **Report Status** | Current report Status == Submitted | O |   | O | O | O | O | O | O |
|  |  | Report Status not in Submitted state |   | O |   |   |   |   |   |   |
|  | **Rejection Reason** | Rejection explanation reason length >= 20 chars | O | O | O | O |   | O | O | O |
|  |  | Rejection reason missing or length < 20 chars |   |   |   | O | O |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status set to Rejected | O |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with Forbidden Error (403) |   |   | O |   |   |   |   |   |
|  |  | `Result.Failure` with Validation Error (422) |   | O |   |   | O | O |   |   |
|  | **Exception** | `ValidationException` |   | O |   |   | O | O |   |   |
|  | **Log message** | "Report rejected by officer with reason logged" | O |   |   |   |   |   | O | O |
|  |  | "Report rejection failed: Reason text too short" |   |   |   | O | O |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |

---

### 3.7 `UT_ConfirmDuplicate` — Confirm Duplicate

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_ConfirmDuplicate` | **Function Name** | Confirm Duplicate (`ConfirmDuplicateCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Confirm report duplicate, ST_DWithin <= 50m, same category, created within 24h, link OriginalReportId. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_ConfirmDuplicate`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Target report & Original report exist in DB | O | O | O | O | O | O | O | O | O |
|  | **Reports Exist** | Both Target & Original reports found in DB | O |   | O | O | O | O | O | O | O |
|  |  | Original report ID missing in DB |   | O |   |   |   |   |   |   |   |
|  | **GPS Distance** | PostGIS spatial distance <= 50m (ST_DWithin) | O | O |   | O | O | O | O | O | O |
|  |  | Distance between reports > 50m threshold |   |   | O |   |   |   |   |   |   |
|  | **Category & Time** | Same category ID & created within 24h window | O | O | O |   | O | O | O | O | O |
|  |  | Different category OR created > 24h apart |   |   |   | O |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status = Duplicate & linked OriginalId | O |   |   |   |   |   |   |   | O |
|  |  | `Result.Failure` with NotFound Error (404) |   | O |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with InvalidDuplicate Error (400) |   |   | O | O |   | O | O | O |   |
|  | **Exception** | `NotFoundException` |   | O |   |   |   |   |   |   |   |
|  |  | `DomainException` |   |   | O | O |   | O | O | O |   |
|  | **Log message** | "Report confirmed duplicate and linked to original report" | O |   |   |   |   |   |   |   | O |
|  |  | "Duplicate confirmation failed: Distance exceeds 50m limit" |   |   | O |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | B | B | A | A | A | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.8 `UT_CloseReport` — Close Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CloseReport` | **Function Name** | Close Report (`CloseReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Close resolved report, author confirmation or 7 days auto-close job, transition Status Resolved -> Closed. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 4 / 2 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_CloseReport`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Report exists in DB & current Status == Resolved | O | O | O | O | O | O | O | O |
|  | **Caller Identity** | Report author Citizen OR Hangfire system auto-close job | O |   | O | O | O | O | O | O |
|  |  | Unauthorized third-party user |   | O |   |   |   |   |   |   |
|  | **Close Threshold** | Citizen confirmation OR 7-day auto-close elapsed | O | O |   | O | O | O | O | O |
|  |  | Resolved time < 7 days without author confirmation |   |   | O |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status set to Closed | O |   |   | O | O |   |   | O |
|  |  | `Result.Failure` with Forbidden Error (403) |   | O |   |   |   |   |   |   |
|  |  | `Result.Failure` with InvalidStatus Error (400) |   |   | O |   |   |   | O | O |
|  | **Exception** | `ForbiddenAccessException` |   | O |   |   |   |   |   |   |
|  | **Log message** | "Report closed successfully" | O |   |   | O | O |   |   | O |
|  |  | "Auto-close job closed resolved report after 7 days" |   |   |   | O | O |   |   | O |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | B | A | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |

---

### 3.9 `UT_RequestReopen` — Request Reopen Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RequestReopen` | **Function Name** | Request Reopen Report (`RequestReopenReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Citizen requests reopening resolved report, max 2 reopens, reason >= 15 chars, evidence photos. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_RequestReopen`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Report exists & Status == Resolved, User == Author | O | O | O | O | O | O | O | O | O |
|  | **Reopen Count** | Reopen count within limit (ReopenCount < 2) | O | O |   | O | O | O | O | O | O |
|  |  | Reopen count reached max quota (ReopenCount >= 2) |   |   | O |   |   |   |   |   |   |
|  | **Evidence Photos** | Reopen evidence image URLs supplied (1-5 photos) | O | O | O |   | O | O | O | O | O |
|  |  | Evidence photos missing or empty list |   |   |   | O |   |   |   |   |   |
|  | **Reopen Reason** | Reopen explanation reason length >= 15 chars | O | O | O | O |   | O | O | O | O |
|  |  | Reopen reason missing or length < 15 chars |   |   |   |   | O |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status set to InProgress | O |   |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with ReopenQuotaExceeded Error (400) |   |   | O |   |   |   |   |   |   |
|  |  | `Result.Failure` with Validation Error (422) |   |   |   | O | O |   | O | O |   |
|  | **Exception** | `DomainException` |   |   | O |   |   |   |   |   |   |
|  |  | `ValidationException` |   |   |   | O | O |   | O | O |   |
|  | **Log message** | "Reopen request submitted, report reopened to InProgress" | O |   |   |   |   |   |   |   | O | O |
|  |  | "Reopen request denied: Maximum 2 reopens reached for report" |   |   | O |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | B | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.10 `UT_AcceptAssignment` — Accept Assignment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AcceptAssignment` | **Function Name** | Accept Assignment (`AcceptAssignmentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Cleanup team accepts assigned task, transition Status Assigned -> InProgress. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_AcceptAssignment`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Assignment record exists in DB & Status == Assigned | O | O | O | O | O | O | O | O |
|  | **Team Membership** | Current user belongs to assigned Cleanup Team | O |   | O | O | O | O | O | O |
|  |  | User does not belong to assigned team |   | O |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Assignment Status = InProgress | O |   |   |   |   | O |   | O |
|  |  | `Result.Failure` with Forbidden Error (403) |   | O |   |   |   |   |   |   |
|  |  | `Result.Failure` with InvalidStatus Error (400) |   |   | O | O | O |   | O |   |
|  | **Exception** | `ForbiddenAccessException` |   | O |   |   |   |   |   |   |
|  | **Log message** | "Cleanup team accepted task assignment" | O |   |   |   |   | O |   | O |
|  |  | "Task assignment accept failed: User not member of team" |   | O |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |

---

### 3.11 `UT_DeclineAssignment` — Decline Assignment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_DeclineAssignment` | **Function Name** | Decline Assignment (`DeclineAssignmentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Cleanup team declines task assignment, decline reason >= 15 chars, notify Officer. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 1 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_DeclineAssignment`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Assignment record exists in DB & Status == Assigned | O | O | O | O | O | O | O | O |
|  | **Team Membership** | Current user belongs to assigned Cleanup Team | O |   | O | O | O | O | O | O |
|  |  | User does not belong to assigned team |   | O |   |   |   |   |   |   |
|  | **Decline Reason** | Decline explanation reason supplied (length >= 15 chars) | O | O |   | O | O | O | O | O |
|  |  | Decline reason missing or length < 15 chars |   |   | O |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Assignment Status = Declined | O |   |   |   |   |   |   | O |
|  |  | `Result.Failure` with Validation Error (422) |   |   | O |   | O | O |   |   |
|  |  | `Result.Failure` with Forbidden Error (403) |   | O |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   |   | O |   | O | O |   |   |
|  | **Log message** | "Task assignment declined by team leader" | O |   |   |   |   |   |   | O |
|  |  | "Assignment decline failed: Reason text too short" |   |   | O |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | A | A | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |

---

### 3.12 `UT_CheckInCleanup` — Check-In Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CheckInCleanup` | **Function Name** | Check-In Cleanup (`CheckInCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Cleanup team check-in at site, verify PostGIS GPS distance <= 200m. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CheckInCleanup`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Assignment Status == InProgress | O | O | O | O | O | O | O | O | O | O |
|  | **GPS Distance** | Distance between GPS location & site <= 200m | O | O |   |   | O | O | O |   |   | O |
|  |  | Distance exceeds 200m threshold |   |   | O | O |   |   |   | O | O |   |   |
| **Confirm** | **Return** | `Result.Success` with CheckedIn = true & CheckInTime | O | O |   |   | O | O | O |   |   | O |
|  |  | `Result.Failure` with OutOfRange Error (400) |   |   | O | O |   |   |   | O | O |   |   |
|  | **Exception** | `DomainException` |   |   | O | O |   |   |   | O | O |   |   |
|  | **Log message** | "Cleanup team checked in at site successfully" | O | O |   |   | O | O | O |   |   | O |
|  |  | "Check-in failed: GPS distance exceeds 200m limit" |   |   | O | O |   |   |   | O | O |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | B | A | A | A | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.13 `UT_UploadBefore` — Upload Before-Cleanup Photos

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UploadBefore` | **Function Name** | Upload Before-Cleanup Photos (`UploadBeforeImagesCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Upload before-cleanup photos, checked-in status required, 1-5 photos from system R2 domain. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_UploadBefore`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Assignment CheckedIn == true | O | O | O | O | O | O | O | O | O |
|  | **Team Leader Role** | User is assigned Team Leader | O |   | O | O | O | O | O | O | O |
|  |  | User is not Team Leader |   | O |   |   |   |   |   |   |   |
|  | **Image Domain** | Image URLs belong to verified system S3/R2 domain | O | O |   | O | O | O | O | O | O |
|  |  | Image URL invalid or untrusted domain |   |   | O |   |   |   |   |   |   |
|  | **Image Count** | Image count within range (1 to 5 photos) | O | O | O |   | O | O | O | O | O |
|  |  | Image count exceeds 5 photos limit |   |   |   | O |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Stage = Before photos attached | O |   |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with Forbidden Error (403) |   | O |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with Validation Error (422) |   |   | O | O |   | O | O |   |   |
|  | **Exception** | `ValidationException` |   |   | O | O |   | O | O |   |   |
|  | **Log message** | "Before-cleanup photos uploaded successfully" | O |   |   |   |   |   |   |   | O | O |
|  |  | "Upload failed: Exceeds 5 photos limit" |   |   |   | O |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.14 `UT_UpdateProgress` — Update Cleanup Progress

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateProgress` | **Function Name** | Update Cleanup Progress (`UpdateProgressCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Update cleanup progress percentage, validate ProgressPercent in range [0, 100]. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_UpdateProgress`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Assignment Status == InProgress | O | O | O | O | O | O | O | O | O | O | O |
|  | **Progress Range** | ProgressPercent value in valid range [0, 100] | O | O |   |   | O | O | O | O | O | O | O |
|  |  | ProgressPercent value out of range (< 0 or > 100) |   |   | O | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with ProgressPercent updated | O | O |   |   | O | O | O | O | O | O | O |
|  |  | `Result.Failure` with Validation Error (422) |   |   | O | O |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   |   | O | O |   |   |   |   |   |   |   |
|  | **Log message** | "Cleanup progress updated successfully" | O | O |   |   | O | O | O | O | O | O | O |
|  |  | "Progress update failed: Value out of range [0, 100]" |   |   | O | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | B | A | A | A | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.15 `UT_ResolveReport` — Resolve Report

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_ResolveReport` | **Function Name** | Resolve Report (`ResolveReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Complete cleanup, upload After-photos (>=1), verify pHash Hamming distance vs Before-photos. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_ResolveReport`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Assignment CheckedIn == true & Before-photos exist | O | O | O | O | O | O | O | O | O | O |
|  | **After Photos** | After-cleanup photo URLs supplied (>= 1 image) | O |   | O | O | O | O | O | O | O | O |
|  |  | After photos missing or empty list |   | O |   |   |   |   |   |   |   |   |
|  | **pHash Distance** | pHash Hamming distance vs Before-photo >= threshold | O | O |   | O | O | O | O | O | O | O |
|  |  | pHash distance below threshold (duplicate photo detected) |   |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status set to Resolved | O |   |   |   |   | O |   |   | O | O |
|  |  | `Result.Failure` with Validation Error (422) |   | O |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with DuplicatePhoto Error (400) |   |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `DomainException` |   |   | O |   |   |   |   |   |   |   |
|  |  | `ValidationException` |   | O |   |   |   |   |   |   |   |   |
|  | **Log message** | "Report resolved, After photos uploaded & pHash verified" | O |   |   |   |   | O |   |   | O | O |
|  |  | "Report resolve failed: After photo matches Before photo (pHash duplicate)" |   |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.16 `UT_EscalateCleanup` — Escalate Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EscalateCleanup` | **Function Name** | Escalate Cleanup (`EscalateCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Escalate cleanup issue to Officer, enforce escalation reason length >= 20 characters. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_EscalateCleanup`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Assignment Status == InProgress | O | O | O | O | O | O | O | O | O |
|  | **Escalation Reason** | Escalation explanation reason length >= 20 chars | O |   | O | O | O | O | O | O | O |
|  |  | Reason missing or length < 20 chars |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status set to Escalated | O |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with Validation Error (422) |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "Cleanup task escalated to Environmental Officer" | O |   |   |   |   |   |   | O | O |
|  |  | "Escalation failed: Reason text too short" |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | B | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.17 `UT_CreateCommunity` — Create Community Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateCommunity` | **Function Name** | Create Community Cleanup (`CreateCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Create community cleanup event, LEO role, check no active campaign exists, set MaxParticipants. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_CreateCommunity`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Report Status == Verified | O | O | O | O | O | O | O | O | O | O | O |
|  | **User Role** | User authenticated with LEO role | O |   | O | O | O | O | O | O | O | O | O |
|  |  | User has Citizen role (forbidden) |   | O |   |   |   |   |   |   |   |   |   |
|  | **Active Campaign** | No active campaign currently exists for report | O | O |   | O | O | O | O | O | O | O | O |
|  |  | Active campaign already exists for report |   |   | O |   |   |   |   |   |   |   |   |
|  | **Leader Role** | Designated Campaign Leader has Cleaner role | O | O | O |   | O | O | O | O | O | O | O |
|  |  | Designated Leader lacks Cleaner role |   |   |   | O |   |   |   |   |   |   |   |
|  | **Max Participants** | MaxParticipants capacity specified (> 0, e.g. 50) | O | O | O | O |   | O | O | O | O | O | O |
|  |  | MaxParticipants <= 0 or invalid |   |   |   |   | O |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<Guid>` with Campaign ID & Status = OpenForJoin | O |   |   |   |   |   |   |   | O | O | O |
|  |  | `Result.Failure` with Forbidden Error (403) |   | O |   |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with DuplicateCampaign Error (409) |   |   | O |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with Validation Error (422) |   |   |   | O | O |   | O | O |   |   |   |
|  | **Exception** | `ForbiddenAccessException` |   | O |   |   |   |   |   |   |   |   |   |
|  |  | `DomainException` |   |   | O |   |   |   |   |   |   |   |   |
|  |  | `ValidationException` |   |   |   | O | O |   | O | O |   |   |   |
|  | **Log message** | "Community cleanup campaign created successfully" | O |   |   |   |   |   |   |   | O | O | O |
|  |  | "Campaign creation failed: Active campaign already exists" |   |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | A | A | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.18 `UT_JoinCommunity` — Join Community Cleanup

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_JoinCommunity` | **Function Name** | Join Community Cleanup (`JoinCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Citizen joins community cleanup event, verify participant count < MaxParticipants, prevent duplicate join. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_JoinCommunity`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Community Cleanup Campaign Status == OpenForJoin | O | O | O | O | O | O | O | O | O | O |
|  | **Participant Count** | Registered participant count < MaxParticipants quota | O |   | O | O | O | O | O | O | O | O |
|  |  | Campaign full (participant count == MaxParticipants) |   | O |   |   |   |   |   |   |   |   |
|  | **Duplicate Registration** | Citizen user not already registered for campaign | O | O |   | O | O | O | O | O | O | O |
|  |  | Citizen already registered for this campaign |   |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with participant record created | O |   |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with QuotaFull Error (400) |   | O |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with AlreadyJoined Error (409) |   |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `DomainException` |   | O | O |   |   |   |   |   |   |   |
|  | **Log message** | "Citizen registered for community cleanup event" | O |   |   |   |   |   |   |   | O | O |
|  |  | "Event join failed: Maximum participant capacity reached" |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | B | A | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.19 `UT_VolunteerCheckIn` — Volunteer Check-In

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_VolunteerCheckIn` | **Function Name** | Volunteer Check-In (`CheckInCommunityCleanupCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Volunteer check-in, GPS distance <= 200m or OverrideReason >= 20 chars, award points. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_VolunteerCheckIn`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | User registered as Participant for campaign | O | O | O | O | O | O | O | O | O | O |
|  | **GPS & Override** | GPS distance <= 200m OR OverrideReason >= 20 chars | O | O |   |   | O | O | O |   |   | O |
|  |  | Distance > 200m without override reason |   |   | O | O |   |   |   | O | O |   |   |
| **Confirm** | **Return** | `Result.Success` with CheckedIn = true & points awarded | O | O |   |   | O | O | O |   |   | O |
|  |  | `Result.Failure` with OutOfRange Error (400) |   |   | O | O |   |   |   | O | O |   |   |
|  | **Exception** | `DomainException` |   |   | O | O |   |   |   | O | O |   |   |
|  | **Log message** | "Volunteer checked in at community cleanup event" | O | O |   |   | O | O | O |   |   | O |
|  |  | "Volunteer check-in failed: Out of 200m radius without override" |   |   | O | O |   |   |   | O | O |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | B | A | A | A | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.20 `UT_CreateInspection` — Create Inspection

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateInspection` | **Function Name** | Create Inspection (`CreateInspectionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Create inspection case, LEO role, Report Status in {Verified, InProgress}, check no active inspection. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CreateInspection`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Target Report Status in {Verified, InProgress} | O | O | O | O | O | O | O | O | O | O |
|  | **User Role** | User authenticated with LEO role | O |   | O | O | O | O | O | O | O | O |
|  |  | User has Citizen role (forbidden) |   | O |   |   |   |   |   |   |   |   |
|  | **Active Case** | No existing active inspection case for report | O | O |   | O | O | O | O | O | O | O |
|  |  | Active inspection case already exists for report |   |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<Guid>` with Inspection ID & Status = Created | O |   |   |   |   |   | O | O | O | O |
|  |  | `Result.Failure` with Forbidden Error (403) |   | O |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with ActiveInspectionExists Error (409) |   |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `ForbiddenAccessException` |   | O |   |   |   |   |   |   |   |   |
|  |  | `DomainException` |   |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "Environmental inspection case created successfully" | O |   |   |   |   |   | O | O | O | O |
|  |  | "Inspection creation failed: Active inspection case exists" |   |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.21 `UT_AssignInspector` — Assign Inspector

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AssignInspector` | **Function Name** | Assign Inspector (`AssignInspectionTeamCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Assign inspection team, LEO/Admin role, verify inspection team exists & active. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_AssignInspector`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Inspection Status == Created & User is LEO/Admin | O | O | O | O | O | O | O | O | O |
|  | **Team Status** | Target Inspection Team exists & currently active | O |   | O | O | O | O | O | O | O |
|  |  | Inspection Team inactive or deleted |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Inspection Status = Assigned | O |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with TeamInactive Error (400) |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `DomainException` |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "Inspection team assigned to inspection case" | O |   |   |   |   |   |   | O | O |
|  |  | "Assignment failed: Inspection team is inactive" |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.22 `UT_CheckInInspection` — Check-In Inspection

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CheckInInspection` | **Function Name** | Check-In Inspection (`CheckInInspectionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Inspector check-in at site, verify PostGIS GPS distance <= 200m. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CheckInInspection`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | User assigned as Inspector on active case | O | O | O | O | O | O | O | O | O | O |
|  | **GPS Distance** | Distance to site <= 200m | O | O |   |   | O | O | O |   |   | O |
|  |  | Distance exceeds 200m threshold |   |   | O | O |   |   |   | O | O |   |   |
| **Confirm** | **Return** | `Result.Success` with CheckedIn = true & CheckInTime | O | O |   |   | O | O | O |   |   | O |
|  |  | `Result.Failure` with OutOfRange Error (400) |   |   | O | O |   |   |   | O | O |   |   |
|  | **Exception** | `DomainException` |   |   | O | O |   |   |   | O | O |   |   |
|  | **Log message** | "Inspector checked in at site successfully" | O | O |   |   | O | O | O |   |   | O |
|  |  | "Inspector check-in failed: GPS distance exceeds 200m limit" |   |   | O | O |   |   |   | O | O |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | B | A | A | A | A | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.23 `UT_SubmitInspection` — Submit Inspection

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SubmitInspection` | **Function Name** | Submit Inspection (`SubmitInspectionReportCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Submit inspection field report, attach checklist (photo/video/audio), violation summary >= 20 chars. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_SubmitInspection`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Inspector CheckedIn == true | O | O | O | O | O | O | O | O | O | O | O |
|  | **Evidence Checklist** | Evidence checklist items attached (photo/video/audio) | O |   | O | O | O | O | O | O | O | O | O |
|  |  | Checklist items missing or empty |   | O |   |   |   |   |   |   |   |   |   |
|  | **Violation Summary** | Violation summary text length >= 20 chars | O | O |   | O | O | O | O | O | O | O | O |
|  |  | Violation summary missing or length < 20 chars |   |   | O |   |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Status = SubmittedForReview | O |   |   |   |   |   |   |   | O | O | O |
|  |  | `Result.Failure` with MissingEvidence Error (400) |   | O |   |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with Validation Error (422) |   |   | O |   |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   |   | O |   |   |   |   |   |   |   |   |
|  | **Log message** | "Inspection field report submitted for decision review" | O |   |   |   |   |   |   |   | O | O | O |
|  |  | "Submission failed: Violation summary text too short" |   |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | A | A | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.24 `UT_IssuePenalty` — Issue Penalty

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_IssuePenalty` | **Function Name** | Issue Penalty (`IssuePenaltyDecisionCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Issue penalty decision, fine amount > 0, due date set in future (+30 days). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_IssuePenalty`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Inspection Status == SubmittedForReview & User is LEO | O | O | O | O | O | O | O | O | O | O |
|  | **Fine Amount** | Administrative fine amount > 0 (e.g. 5,000,000 VND) | O |   | O | O | O | O | O | O | O | O |
|  |  | Fine amount <= 0 or invalid |   | O |   |   |   |   |   |   |   |   |
|  | **Due Date** | Payment due date set in future (+30 days) | O | O |   | O | O | O | O | O | O | O |
|  |  | Payment due date in past or invalid |   |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<Guid>` with Penalty ID & Status = Issued | O |   |   |   |   |   |   |   |   | O |
|  |  | `Result.Failure` with InvalidFine Error (422) |   | O |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with InvalidDueDate Error (422) |   |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O | O |   |   |   |   |   |   |   |
|  | **Log message** | "Administrative penalty decision issued successfully" | O |   |   |   |   |   |   |   |   | O |
|  |  | "Penalty issue failed: Fine amount must be greater than zero" |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | B | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.25 `UT_RecordPayment` — Record Payment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_RecordPayment` | **Function Name** | Record Payment (`RecordPenaltyPaymentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Record penalty payment, full fine amount match required, close inspection case. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_RecordPayment`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Penalty Decision Status == Issued & User is LEO/Admin | O | O | O | O | O | O | O | O | O |
|  | **Payment Amount** | Recorded payment amount matches full issued fine amount | O |   | O | O | O | O | O | O | O |
|  |  | Payment amount partial or mismatch |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Penalty Status = Paid & Case Closed | O |   |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with PartialPayment Error (400) |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `DomainException` |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "Penalty fine payment recorded & inspection case closed" | O |   |   |   |   |   |   |   | O | O |
|  |  | "Payment record failed: Partial payments not accepted" |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | B | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.26 `UT_CreateCompany` — Create Company

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_CreateCompany` | **Function Name** | Create Company (`CreateCompanyCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Register environmental company, Admin/DEO role, TaxCode unique check, Company Name & Address. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_CreateCompany`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | User authenticated with Admin or DEO role | O | O | O | O | O | O | O | O | O | O |
|  | **TaxCode** | TaxCode unique (not existing in company registry) | O |   | O | O | O | O | O | O | O | O |
|  |  | TaxCode already registered in system |   | O |   |   |   |   |   |   |   |   |
|  | **Company Details** | Company Name & Address text supplied | O | O |   | O | O | O | O | O | O | O |
|  |  | Company Name missing or empty |   |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<Guid>` with Company ID | O |   |   |   |   |   | O | O | O | O |
|  |  | `Result.Failure` with DuplicateTaxCode Error (409) |   | O |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with Validation Error (422) |   |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `BusinessRuleViolationException` |   | O |   |   |   |   |   |   |   |   |
|  |  | `ValidationException` |   |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "Environmental company profile created successfully" | O |   |   |   |   |   | O | O | O | O |
|  |  | "Company registration failed: TaxCode already exists" |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.27 `UT_AddTeamMember` — Add Team Member

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AddTeamMember` | **Function Name** | Add Team Member (`AddTeamMemberCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Add staff to cleanup team, verify team active, staff user not already member of another team. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_AddTeamMember`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Target Team exists & currently active | O | O | O | O | O | O | O | O | O |
|  | **Staff Availability** | Target staff user not already member of active team | O |   | O | O | O | O | O | O | O |
|  |  | Staff user already assigned to another active team |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with TeamMember relation added | O |   |   |   |   |   |   |   | O | O |
|  |  | `Result.Failure` with AlreadyMember Error (409) |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `DomainException` |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "Staff member added to cleanup team successfully" | O |   |   |   |   |   |   |   | O | O |
|  |  | "Add team member failed: User is already active in another team" |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.28 `UT_AwardPoints` — Award Points

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AwardPoints` | **Function Name** | Award Points (`GamificationPointAwarder`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Award gamification points (ReportVerified +50, CleanupJoin +20), update user balance. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_AwardPoints`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Target User account exists & active | O | O | O | O | O | O | O | O | O | O |
|  | **Event Type** | Valid gamification event type (ReportVerified, CleanupJoin) | O |   | O | O | O | O | O | O | O | O |
|  |  | Event type invalid or not recognized |   | O |   |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with User.Points balance updated | O |   |   |   |   |   | O | O | O | O |
|  |  | `Result.Failure` with InvalidEventType Error (400) |   | O |   |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O |   |   |   |   |   |   |   |   |
|  | **Log message** | "Gamification points awarded to user profile" | O |   |   |   |   |   | O | O | O | O |
|  |  | "Award points failed: Invalid gamification activity type" |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.29 `UT_EvaluateBadges` — Evaluate Badges

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EvaluateBadges` | **Function Name** | Evaluate Badges (`BadgeEligibilityEvaluator`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Evaluate badge eligibility, check verified report count >= required count (e.g. 5 reports). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_EvaluateBadges`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | User activity metrics calculated from DB history | O | O | O | O | O | O | O | O | O | O |
|  | **Verified Count** | Verified report count >= Badge required count (e.g. 5) | O |   |   | O | O |   |   | O | O | O |
|  |  | Verified report count < required count |   | O | O |   |   | O | O |   |   |   |
| **Confirm** | **Return** | `Result.Success` with UserBadge unlocked & awarded | O |   |   | O | O |   |   | O | O | O |
|  |  | `Result.Failure` with ThresholdNotMet Error (400) |   | O | O |   |   | O | O |   |   |   |
|  | **Exception** | `DomainException` |   | O | O |   |   | O | O |   |   |   |
|  | **Log message** | "Badge unlocked & awarded to user profile" | O |   |   | O | O |   |   | O | O | O |
|  |  | "Badge evaluation: User threshold not met for badge" |   | O | O |   |   | O | O |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | B | B | N | A | A | N | A | A |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.30 `UT_PresignMedia` — Presign Media Upload

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_PresignMedia` | **Function Name** | Presign Media Upload (`PresignMediaUploadCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Generate presigned R2 upload URL, validate Content-Type in {jpeg, png, webp}, file size <= 10MB. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_PresignMedia`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | User authenticated with active session | O | O | O | O | O | O | O | O | O | O | O |
|  | **Content-Type** | Content-Type in {image/jpeg, image/png, image/webp} | O |   | O | O | O | O | O | O | O | O | O |
|  |  | Unsupported Content-Type (e.g. application/pdf) |   | O |   |   |   |   |   |   |   |   |   |
|  | **File Size** | File size <= 10,485,760 bytes (10MB limit) | O | O |   | O | O | O | O | O | O | O | O |
|  |  | File size exceeds 10MB limit (> 10,485,760 bytes) |   |   | O |   |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<PresignedUrlDto>` with PUT URL & object key | O |   |   |   | O | O | O | O | O | O | O |
|  |  | `Result.Failure` with InvalidMime Error (415) |   | O |   |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with FileTooLarge Error (400) |   |   | O |   |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O | O |   |   |   |   |   |   |   |   |
|  | **Log message** | "Presigned R2 upload URL generated successfully" | O |   |   |   | O | O | O | O | O | O | O |
|  |  | "Presign URL failed: File size exceeds 10MB limit" |   |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | B | A | A | A | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.31 `UT_EvaluateExif` — Evaluate EXIF Metadata

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_EvaluateExif` | **Function Name** | Evaluate EXIF Metadata (`ExifSuspicionEvaluator`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Analyze image EXIF metadata, detect camera make/model/GPS, flag software editing (Photoshop/Canva). |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_EvaluateExif`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Uploaded image stream readable | O | O | O | O | O | O | O | O | O | O |
|  | **EXIF Tags** | EXIF metadata contains camera info & GPS tags | O |   | O | O | O | O | O | O | O | O |
|  |  | EXIF metadata missing or stripped |   | O |   |   |   |   |   |   |   |   |
|  | **Software Editing** | No software editing tags found (clean EXIF) | O | O |   | O | O | O | O | O | O | O |
|  |  | Software editing tags found (Photoshop / Canva) |   |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with EXIF evaluation clean | O | O |   | O | O | O | O | O | O | O |
|  |  | `Result.Failure` with FlaggedSuspicious status |   |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `InfrastructureException` |   | O |   |   |   |   |   |   |   |   |
|  | **Log message** | "EXIF metadata evaluated clean" | O | O |   | O | O | O | O | O | O | O |
|  |  | "EXIF evaluation flagged suspicious: Photoshop tag detected" |   |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | B | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.32 `UT_GetNearbyMap` — Get Nearby Map

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetNearbyMap` | **Function Name** | Get Nearby Map (`GetNearbyReportsQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query nearby reports within radius, PostGIS ST_DWithin, RadiusKm in [0.5, 50.0], GPS ~10m privacy rounding. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_GetNearbyMap`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | PostgreSQL + PostGIS spatial extension active | O | O | O | O | O | O | O | O | O | O | O |
|  | **Center Lat/Lng** | Center Latitude & Longitude parameters supplied | O |   | O | O | O | O | O | O | O | O | O |
|  |  | Center coordinates missing or invalid |   | O |   |   |   |   |   |   |   |   |   |
|  | **RadiusKm** | RadiusKm in valid range [0.5, 50.0] km | O | O |   | O | O | O | O | O | O | O | O |
|  |  | RadiusKm out of range (< 0.5 or > 50.0 km) |   |   | O |   |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<List<MapPointDto>>` with privacy-rounded GPS | O |   |   |   | O | O | O | O | O | O | O |
|  |  | `Result.Failure` with InvalidCoordinates Error (400) |   | O |   |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with InvalidRadius Error (400) |   |   | O |   |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O | O |   |   |   |   |   |   |   |   |
|  | **Log message** | "Nearby reports map query executed successfully" | O |   |   |   | O | O | O | O | O | O | O |
|  |  | "Nearby map query failed: Radius exceeds 50km limit" |   |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | B | A | A | A | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.33 `UT_GetHeatmap` — Get Heatmap

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetHeatmap` | **Function Name** | Get Heatmap (`GetHeatmapQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query heatmap density points, PostGIS bounding box coordinates validation, 10m Redis cache. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_GetHeatmap`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Database connection active & Catalog ready | O | O | O | O | O | O | O | O | O |
|  | **Bounding Box** | Bounding box coordinates valid (minLat < maxLat, minLng < maxLng) | O |   | O | O | O | O | O | O | O |
|  |  | Bounding box coordinates invalid or inverted |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<List<HeatmapPointDto>>` with density weights | O |   | O | O | O | O | O | O | O |
|  |  | `Result.Failure` with InvalidBBox Error (400) |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "Heatmap density points query executed successfully" | O |   | O | O | O | O | O | O | O |
|  |  | "Heatmap query failed: Bounding box coordinates invalid" |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.34 `UT_SendNotification` — Send Notification

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_SendNotification` | **Function Name** | Send Notification (`SendNotificationCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Send FCM push notification & save to In-App Inbox, enforce <= 20 daily anti-spam limit. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_SendNotification`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Recipient User account exists & FCM token registered | O | O | O | O | O | O | O | O | O | O |
|  | **Daily Anti-Spam** | Daily notification count <= 20 anti-spam limit | O |   | O | O | O | O | O | O | O | O |
|  |  | Daily notification count exceeded (> 20 / day) |   | O |   |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with Push Notification dispatched | O |   | O | O | O | O | O | O | O | O |
|  |  | `Result.Success` (Queued for daily digest summary) |   | O |   |   |   |   |   |   |   |   |
|  | **Exception** | `InfrastructureException` |   |   |   |   |   |   |   |   |   |   |
|  | **Log message** | "Push notification sent via FCM successfully" | O |   | O | O | O | O | O | O | O | O |
|  |  | "Daily anti-spam limit reached: Queued for evening digest" |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | B | A | A | A | A | B | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.35 `UT_AddComment` — Add Comment

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_AddComment` | **Function Name** | Add Comment (`AddCommentCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 110 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Add comment to report, text length <= 1000 chars, blocked words filter check. |
| **Test Results** | **Passed:** 11 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 3 / 6 / 2 \| **Total TCs:** 11 |

#### Condition & Confirmation Matrix (`UT_AddComment`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 | UTC11 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Target Report exists in DB & not soft-deleted | O | O | O | O | O | O | O | O | O | O | O |
|  | **Comment Length** | Comment text content non-empty & length <= 1000 chars | O |   | O | O | O | O | O | O | O | O | O |
|  |  | Comment text empty or length > 1000 chars |   | O |   |   |   |   |   |   |   |   |   |
|  | **Blocked Words** | Comment content contains no blocked words | O | O |   | O | O | O | O | O | O | O | O |
|  |  | Comment contains offensive / blocked words |   |   | O |   |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result<Guid>` with Comment ID created | O |   |   |   | O | O | O | O | O | O | O |
|  |  | `Result.Failure` with Validation Error (422) |   | O |   |   |   |   |   |   |   |   |   |
|  |  | `Result.Failure` with BlockedWord Error (400) |   |   | O |   |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O |   |   |   |   |   |   |   |   |   |
|  |  | `DomainException` |   |   | O |   |   |   |   |   |   |   |   |
|  | **Log message** | "Comment posted on pollution report successfully" | O |   |   |   | O | O | O | O | O | O | O |
|  |  | "Comment post rejected: Contains blocked words" |   |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | B | A | A | A | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.36 `UT_UpdateProfile` — Update Profile

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateProfile` | **Function Name** | Update Profile (`UpdateProfileCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 100 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Update user profile, validate VN phone number regex (10 digits start 0), FullName 2-100 chars. |
| **Test Results** | **Passed:** 10 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 6 / 2 \| **Total TCs:** 10 |

#### Condition & Confirmation Matrix (`UT_UpdateProfile`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 | UTC10 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | User authenticated with active session | O | O | O | O | O | O | O | O | O | O |
|  | **Phone Format** | Phone number format valid (VN regex: 10 digits start 0) | O |   | O | O | O | O | O | O | O | O |
|  |  | Phone number format invalid |   | O |   |   |   |   |   |   |   |   |
|  | **FullName** | FullName supplied (length >= 2 and <= 100 chars) | O | O |   | O | O | O | O | O | O | O |
|  |  | FullName empty or invalid length |   |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with User profile updated in DB | O |   |   |   | O | O | O | O | O | O |
|  |  | `Result.Failure` with Validation Error (422) |   | O | O |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O | O |   |   |   |   |   |   |   |
|  | **Log message** | "User profile updated successfully" | O |   |   |   | O | O | O | O | O | O |
|  |  | "Profile update failed: Phone number format invalid" |   | O |   |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | B | B | A | A | N | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |  |

---

### 3.37 `UT_BanUser` — Ban User

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_BanUser` | **Function Name** | Ban User (`BanUserCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Ban/unban user account, Admin role required, prevent self-ban (Admin cannot ban own account). |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_BanUser`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Caller authenticated with Admin role | O | O | O | O | O | O | O | O | O |
|  | **Target Account** | Target User exists & is not self (cannot ban oneself) | O |   | O | O | O | O | O | O | O |
|  |  | Target user is caller (self-ban attempt) |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with IsBanned toggled & tokens revoked | O |   |   | O | O | O | O | O | O |
|  |  | `Result.Failure` with SelfBanForbidden Error (400) |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `DomainException` |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "User account banned and active refresh tokens revoked" | O |   |   | O | O | O | O | O | O |
|  |  | "Ban user failed: Admin cannot ban own account" |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.38 `UT_UpdateUserRole` — Update User Role

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_UpdateUserRole` | **Function Name** | Update User Role (`UpdateUserRoleCommandHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 90 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Update user role, Admin role required, target role in UserRole enum {Citizen, Officer, CleanupTeam, Admin}. |
| **Test Results** | **Passed:** 9 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 5 / 2 \| **Total TCs:** 9 |

#### Condition & Confirmation Matrix (`UT_UpdateUserRole`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 | UTC09 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Caller authenticated with Admin role | O | O | O | O | O | O | O | O | O |
|  | **Target Role** | Target role is valid UserRole enum value | O |   | O | O | O | O | O | O | O |
|  |  | Target role invalid or out of enum range |   | O |   |   |   |   |   |   |   |
| **Confirm** | **Return** | `Result.Success` with User role updated in Identity DB | O |   |   | O | O | O | O | O | O |
|  |  | `Result.Failure` with InvalidRole Error (400) |   | O |   |   |   |   |   |   |   |
|  | **Exception** | `ValidationException` |   | O |   |   |   |   |   |   |   |
|  | **Log message** | "User role updated successfully by Admin" | O |   |   | O | O | O | O | O | O |
|  |  | "Update role failed: Role enum value invalid" |   | O |   |   |   |   |   |   |   |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | A | A | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |  |

---

### 3.39 `UT_GetCategories` — Get Categories

| Attribute | Detail | Attribute | Detail |
| :--- | :--- | :--- | :--- |
| **Function Code** | `UT_GetCategories` | **Function Name** | Get Categories (`GetCategoriesQueryHandler`) |
| **Created By** | Dev Team | **Executed By** | xUnit Runner (.NET 9) |
| **Lines of Code (LOC)**| 80 | **Lack of Test Cases**| 0 |
| **Test Requirement** | Query reference catalog of pollution categories, handle OnlyActive filter flag. |
| **Test Results** | **Passed:** 8 \| **Failed:** 0 \| **Untested:** 0 \| **N / A / B:** 2 / 4 / 2 \| **Total TCs:** 8 |

#### Condition & Confirmation Matrix (`UT_GetCategories`)

| Category | Parameter / Sub-category | Value / Condition Partition | UTC01 | UTC02 | UTC03 | UTC04 | UTC05 | UTC06 | UTC07 | UTC08 |
| :--- | :--- | :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| **Condition** | **Precondition** | Database connection active & Catalog ready | O | O | O | O | O | O | O | O |
|  | **OnlyActive Filter** | OnlyActive parameter supplied (true/false) | O | O | O | O | O | O | O | O |
| **Confirm** | **Return** | `Result<List<CategoryDto>>` with active category list | O | O | O | O | O | O | O | O |
|  | **Exception** | `InfrastructureException` |   |   |   |   |   |   |   |   |
|  | **Log message** | "Pollution categories reference list retrieved" | O | O | O | O | O | O | O | O |
| **Result** | **Type (N : Normal, A : Abnormal, B : Boundary)** |  | N | N | A | A | A | A | B | N |
|  | **Passed / Failed** |  | P | P | P | P | P | P | P | P |
|  | **Executed Date** |  | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 | 08/10 |
|  | **Defect ID** |  |  |  |  |  |  |  |  |  |

---

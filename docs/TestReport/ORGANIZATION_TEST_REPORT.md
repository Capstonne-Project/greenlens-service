# Test Report — Organization (Teams)

|                      |                                    |
| -------------------- | ---------------------------------- |
| **Feature**          | **Organization — Teams Management**|
| **Test requirement** |                                    |
| **Number of TCs**    | **98**                             |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 96     | 2      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## My Team Profile

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_001 | View my team profile — Cleaner. | 1. Login as Cleaner.<br>2. Navigate to "Team của tôi". | Team detail displayed: team name, type, office, members list with role (leader/member). | - User is Cleaner and belongs to a team. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams/my-profile`. Returns team + member list. |
| TC_ORG_002 | View my team profile — CompanyStaff. | 1. Login as CompanyStaff.<br>2. Navigate to "Team của tôi". | Team detail displayed (company team, no LocalOffice). | - User is CompanyStaff and belongs to a team. | Passed | 04/09/2026 | TamKnm | | | | | | | Works for both community and company teams. |
| TC_ORG_003 | View my team profile — Inspector. | 1. Login as Inspector.<br>2. Navigate to "Team của tôi". | Team detail displayed for Inspection team. | - User is Inspector and belongs to a team. | Passed | 04/09/2026 | TamKnm | | | | | | | Inspector sees their Inspection team. |
| TC_ORG_004 | View my team profile — not in any team. | 1. Login as Cleaner who is not assigned to any team.<br>2. Navigate to "Team của tôi". | Error 404 "Chưa thuộc team nào" is displayed. | - User has no team membership. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns `Errors.Organization.NotInAnyTeam` or similar. |
| TC_ORG_005 | View my team profile — unauthorized role (Citizen). | 1. Login as Citizen.<br>2. Attempt to access my team profile. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Cleaner,CompanyStaff,Inspector")]` blocks Citizen. |

---

## My Tasks — View Assignments

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_006 | View my tasks — all statuses. | 1. Login as Cleaner.<br>2. Navigate to "Nhiệm vụ của tôi". | All tasks displayed: Assigned, InProgress, Completed, Declined. Default page=1, pageSize=20. | - User is Cleaner with team. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams/my-tasks` without status filter. |
| TC_ORG_007 | View my tasks — filter by Assigned status. | 1. Login as Cleaner.<br>2. Navigate to "Nhiệm vụ".<br>3. Filter by "Chờ xác nhận". | Only tasks with status Assigned are shown. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `assignmentStatus=Assigned`. |
| TC_ORG_008 | View my tasks — filter by InProgress status. | 1. Login as Cleaner.<br>2. Filter by "Đang thực hiện". | Only InProgress tasks are shown. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `assignmentStatus=InProgress`. |
| TC_ORG_009 | View my tasks — pagination. | 1. Login as Cleaner.<br>2. Navigate to page 2 with pageSize=5. | Second page displayed with max 5 items. | - User has >5 tasks. | Passed | 04/09/2026 | TamKnm | | | | | | | Pagination via `page` and `pageSize` query params. |
| TC_ORG_010 | View my task detail — success. | 1. Login as Cleaner.<br>2. Click on a specific task. | Task detail displayed: report info, before images, current progress, SLA deadline, available actions (canDecline, canUpdateProgress, canResolve). | - User is Cleaner with assigned task. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams/my-tasks/{reportId}`. All team members can view. |
| TC_ORG_011 | View my task detail — task not found. | 1. Login as Cleaner.<br>2. Click on a report that is not assigned to the team. | Error 404 "Không tìm thấy assignment" is displayed. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler returns NotFound when no assignment found for team+report. |
| TC_ORG_012 | View my task detail — not in any team. | 1. Login as Cleaner without team.<br>2. Attempt to view task detail. | Error 422 "User không thuộc team nào" is displayed. | - User has no team membership. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks user has a team before querying. |

---

## My Tasks — Progress Stats

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_013 | View progress stats — success. | 1. Login as Cleaner.<br>2. Navigate to "Tiến độ" dashboard. | Stats displayed: task distribution by status, by severity, SLA breach count, 30-day completion trend. | - User is Cleaner with team. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams/my-tasks/progress-stats`. |
| TC_ORG_014 | View progress stats — Citizen cannot access. | 1. Login as Citizen.<br>2. Attempt to access progress stats. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]`. |

---

## Accept Assignment

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_015 | Accept task — success. | 1. Login as Team Leader (Cleaner).<br>2. Mở task với status "Chờ xác nhận".<br>3. Click "Nhận task". | Success "Đã chấp nhận task." Assignment status: Assigned → InProgress. StartedAt is set. | - User is Team Leader.<br>- Assignment is Assigned. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates leader, transitions assignment to InProgress. |
| TC_ORG_016 | Accept task — not team leader. | 1. Login as regular Cleaner (not leader).<br>2. Click "Nhận task". | Error "Không phải leader hoặc assignment không ở trạng thái Assigned" is displayed. | - User is team member but not leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks leader role before accepting. |
| TC_ORG_017 | Accept task — wrong status (already InProgress). | 1. Login as Team Leader.<br>2. Try to accept a task already InProgress. | Error indicating invalid status transition is displayed. | - Assignment already InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks assignment status. |
| TC_ORG_018 | Accept task — idempotent (double click). | 1. Login as Team Leader.<br>2. Click "Nhận task" twice rapidly. | Only one transition. No error on second call. | - Assignment is Assigned. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller has `[SupportsIdempotency]` attribute. |

---

## Decline Assignment

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_019 | Decline task — success (within 24h). | 1. Login as Cleaner.<br>2. Mở task mới giao (< 24h).<br>3. Click "Từ chối".<br>4. Nhập lý do ≥ 20 ký tự.<br>5. Xác nhận. | Success "Đã từ chối task." Report stays InProgress. LEO sees declined status. | - Assignment is Assigned.<br>- Within 24h of assignment. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates 24h window and reason length. |
| TC_ORG_020 | Decline task — past 24h window. | 1. Login as Cleaner.<br>2. Try to decline task assigned >24h ago. | Error "Quá 24h" is displayed. | - Assignment created >24h ago. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks time window. |
| TC_ORG_021 | Decline task — reason too short (< 20 chars). | 1. Login as Cleaner.<br>2. Nhập lý do < 20 ký tự.<br>3. Xác nhận. | Error "Lý do quá ngắn" is displayed. Min 20 ký tự. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates reason minimum length. |
| TC_ORG_022 | Decline task — wrong status. | 1. Login as Cleaner.<br>2. Try to decline an InProgress task. | Error indicating invalid status is displayed. | - Assignment is InProgress (already accepted). | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks assignment status must be Assigned. |

---

## Check-In Cleanup

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_023 | Check-in — within 200m success. | 1. Login as Cleaner.<br>2. Mở task InProgress.<br>3. App tự detect GPS ≤ 200m từ vị trí báo cáo.<br>4. Click "Check-in". | Success "Đã check-in hiện trường." GPS recorded. | - Assignment is Assigned/InProgress.<br>- Cleaner within 200m. | Passed | 04/09/2026 | TamKnm | | | | | | | PostGIS `ST_DWithin` validates distance ≤ 200m. |
| TC_ORG_024 | Check-in — beyond 200m. | 1. Login as Cleaner.<br>2. GPS > 200m từ báo cáo.<br>3. Click "Check-in". | Error "Quá xa hiện trường" is displayed. Distance threshold 200m. | - Cleaner >200m from report. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler rejects check-in when too far. |
| TC_ORG_025 | Check-in — Inspector cannot check-in. | 1. Login as Inspector.<br>2. Try to check-in for cleanup task. | 403 Forbidden is returned. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Cleaner,CompanyStaff")]` blocks Inspector. |
| TC_ORG_026 | Check-in — idempotent. | 1. Login as Cleaner.<br>2. Click "Check-in" twice. | No error on second call. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller has `[SupportsIdempotency]`. |

---

## Update Cleanup Progress

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_027 | Update progress — success (50%). | 1. Login as Cleaner.<br>2. Mở task InProgress.<br>3. Cập nhật tiến độ = 50%.<br>4. Click "Lưu". | Success "Đã cập nhật tiến độ." Progress = 50%. | - Assignment is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates percent 0–100 and InProgress status. |
| TC_ORG_028 | Update progress — percent > 100. | 1. Login as Cleaner.<br>2. Nhập tiến độ = 150%. | Error "Percent ngoài 0–100" is displayed. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator checks percent ≤ 100. |
| TC_ORG_029 | Update progress — percent < 0. | 1. Login as Cleaner.<br>2. Nhập tiến độ = -10%. | Error indicating invalid percent is displayed. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator checks percent ≥ 0. |
| TC_ORG_030 | Update progress — not InProgress. | 1. Login as Cleaner.<br>2. Try to update progress on Completed task. | Error indicating invalid status is displayed. | - Assignment is Completed. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks assignment is InProgress. |
| TC_ORG_031 | Update progress — optional note. | 1. Login as Cleaner.<br>2. Update progress with note "Đã dọn 50%".<br>3. Click "Lưu". | Success. Note saved with progress record. | - Assignment is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Note is optional field in request. |

---

## Escalate Cleanup

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_032 | Escalate — success. | 1. Login as Cleaner.<br>2. Mở task InProgress.<br>3. Click "Báo cáo vượt khả năng".<br>4. Nhập lý do ≥ 20 ký tự.<br>5. Xác nhận. | Success "Đã escalate lên LEO." Nếu tất cả team đều escalate → report quay về Verified. | - Assignment is InProgress.<br>- Reason ≥ 20 chars. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler escalates to LEO. |
| TC_ORG_033 | Escalate — reason too short. | 1. Login as Cleaner.<br>2. Nhập lý do < 20 ký tự. | Error "Lý do quá ngắn" is displayed. Min 20 ký tự. | - User is Cleaner. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates reason length. |
| TC_ORG_034 | Escalate — not InProgress. | 1. Login as Cleaner.<br>2. Try to escalate Assigned task. | Error indicating invalid status is displayed. | - Assignment is Assigned. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks InProgress status. |

---

## My Progress History

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_035 | View progress history — Team Leader. | 1. Login as Team Leader (Cleaner).<br>2. Navigate to "Lịch sử tiến độ". | Progress history displayed with pagination. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams/my-progress`. |
| TC_ORG_036 | View progress history — not Team Leader. | 1. Login as regular Cleaner (not leader).<br>2. Try to access progress history. | Error 422 "User không phải Team Leader" is displayed. | - User is Cleaner but not leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates leader role. |
| TC_ORG_037 | View progress history — filter by status. | 1. Login as Team Leader.<br>2. Filter by assignmentStatus=Completed. | Only completed task progress records shown. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `assignmentStatus`. |

---

## Community Team CRUD (LEO)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_038 | List community teams — success. | 1. Login as LEO.<br>2. Navigate to "Quản lý đội". | List of community teams (CompanyId == null) in LEO's office. Status Available/Busy shown. | - User is LEO with assigned office. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams`. Filters: teamType, isActive, isAvailable. |
| TC_ORG_039 | List teams — filter by Cleanup type. | 1. Login as LEO.<br>2. Filter by teamType=Cleanup. | Only Cleanup teams shown. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `teamType=Cleanup`. |
| TC_ORG_040 | List teams — filter by availability. | 1. Login as LEO.<br>2. Filter by isAvailable=true. | Only available teams (not busy) shown. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `isAvailable=true`. |
| TC_ORG_041 | List teams — Citizen cannot access. | 1. Login as Citizen.<br>2. Attempt to access team list. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Admin,LEO,DEO")]` blocks Citizen. |
| TC_ORG_042 | View team detail — success. | 1. Login as LEO.<br>2. Click on a team in the list. | Team detail page: name, type, office, member list (name, email, role, leader status). | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams/{id}`. |
| TC_ORG_043 | View team detail — team not found. | 1. Login as LEO.<br>2. Navigate to non-existent team ID. | Error 404 "Không tìm thấy" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `team is null`. |
| TC_ORG_044 | View team detail — team outside LEO scope. | 1. Login as LEO of Ward A.<br>2. Try to view team belonging to Ward B. | Error 422 "Team không thuộc phạm vi quản lý" or 403 is displayed. | - User is LEO.<br>- Team is in different office. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates scope access. |
| TC_ORG_045 | Create team — success (Cleanup). | 1. Login as LEO.<br>2. Click "Tạo team mới".<br>3. Nhập tên "Đội vệ sinh 1".<br>4. Chọn loại Cleanup.<br>5. Xác nhận. | Success "Đã tạo team thành công." Team created under LEO's office. | - User is LEO with assigned office. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler auto-resolves LocalOfficeId from token. |
| TC_ORG_046 | Create team — success (Inspection). | 1. Login as LEO.<br>2. Nhập tên "Đội thanh tra 1".<br>3. Chọn loại Inspection.<br>4. Xác nhận. | Success. Inspection team created. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | TeamType Inspection allowed for community teams. |
| TC_ORG_047 | Create team — empty name. | 1. Login as LEO.<br>2. Để trống tên team.<br>3. Xác nhận. | Error "Tên team là bắt buộc." is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for Name. |
| TC_ORG_048 | Create team — name too long (> 100 chars). | 1. Login as LEO.<br>2. Nhập tên > 100 ký tự. | Error indicating max length exceeded is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(100)`. |
| TC_ORG_049 | Create team — invalid team type. | 1. Login as LEO.<br>2. Submit invalid TeamType value. | Error "TeamType phải là Cleanup hoặc Inspection." is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `Must(t => t is Cleanup or Inspection)`. |
| TC_ORG_050 | Create team — LEO without office. | 1. Login as LEO not assigned to any office.<br>2. Try to create team. | Error 404 "LEO chưa được gán office" is displayed. | - LEO has no assigned office. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `office is null`. |
| TC_ORG_051 | Create team — Citizen cannot create. | 1. Login as Citizen.<br>2. Attempt to create team. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO")]` blocks other roles. |
| TC_ORG_052 | Update team — success. | 1. Login as LEO.<br>2. Mở team detail.<br>3. Đổi tên thành "Đội vệ sinh A".<br>4. Click "Lưu". | Success "Đã cập nhật team." Name updated. | - User is LEO.<br>- Team belongs to LEO's office. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `PUT /v1/teams/{id}`. |
| TC_ORG_053 | Update team — team not found. | 1. Login as LEO.<br>2. Try to update non-existent team. | Error 404 "Không tìm thấy" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks team exists. |

---

## Community Team Members (LEO)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_054 | Add member to team — Cleaner to Cleanup team. | 1. Login as LEO.<br>2. Mở team Cleanup.<br>3. Click "Thêm thành viên".<br>4. Chọn user có role Cleaner thuộc cùng phường.<br>5. Xác nhận. | Success "Đã thêm thành viên vào team." Member appears in list. | - User is LEO.<br>- Target user is Cleaner in same office. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates role, office, existing membership. |
| TC_ORG_055 | Add member — Inspector to Inspection team. | 1. Login as LEO.<br>2. Mở team Inspection.<br>3. Thêm user có role Inspector. | Success. Inspector added to Inspection team. | - User is LEO.<br>- Target user is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Role Inspector matches TeamType Inspection. |
| TC_ORG_056 | Add member — wrong role (Cleaner to Inspection team). | 1. Login as LEO.<br>2. Try to add Cleaner to Inspection team. | Error "Role không khớp team" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | `validRole` check: Cleaner → Cleanup only, Inspector → Inspection only. |
| TC_ORG_057 | Add member — user not in LEO's office. | 1. Login as LEO of Ward A.<br>2. Try to add user from Ward B. | Error "User không thuộc phường" is displayed. | - User is from different office. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user.LocalOfficeId != team.LocalOfficeId`. |
| TC_ORG_058 | Add member — user already in this team (duplicate). | 1. Login as LEO.<br>2. Try to add member who is already in the team. | Error 409 "User đã trong team" is displayed. | - User already member. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks existing membership → `Errors.Organization.MemberAlreadyInTeam`. |
| TC_ORG_059 | Add member — user already in another team. | 1. Login as LEO.<br>2. Try to add member who belongs to another team. | Error "User đã thuộc team khác" is displayed. | - User in a different team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `existingMembership.TeamId != request.TeamId` → `Errors.Organization.UserAlreadyInTeam`. |
| TC_ORG_060 | Add member — as leader when team already has leader. | 1. Login as LEO.<br>2. Try to add user as leader to a team that already has a leader. | Error "Team đã có leader" is displayed. | - Team already has a leader. | Passed | 04/09/2026 | TamKnm | | | | | | | `TeamMembershipRules.TeamHasLeaderAsync` check. |
| TC_ORG_061 | Add member — user not found. | 1. Login as LEO.<br>2. Try to add non-existent user ID. | Error 404 "Không tìm thấy user" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `user is null`. |
| TC_ORG_062 | Add member — team not found. | 1. Login as LEO.<br>2. Try to add member to non-existent team. | Error 404 "Không tìm thấy team" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `team is null`. |
| TC_ORG_063 | Remove member — success. | 1. Login as LEO.<br>2. Mở team detail.<br>3. Click "Xóa" bên cạnh thành viên.<br>4. Xác nhận. | Success "Đã xóa thành viên khỏi team." User still exists in system. | - User is LEO.<br>- Member exists in team. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `DELETE /v1/teams/{teamId}/members/{userId}`. User retains role. |
| TC_ORG_064 | Remove member — member not found. | 1. Login as LEO.<br>2. Try to remove non-existent member. | Error 404 "Không tìm thấy thành viên" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks member exists. |

---

## Transfer Team Member (LEO)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_065 | Transfer member — success. | 1. Login as LEO.<br>2. Mở team A detail.<br>3. Click "Chuyển" bên cạnh thành viên.<br>4. Chọn team B (cùng office, cùng loại).<br>5. Xác nhận. | Success "Đã chuyển thành viên sang team mới." User removed from A, added to B. | - Both teams in LEO's office.<br>- Role matches new team type. | Passed | 04/09/2026 | TamKnm | | | | | | | Atomic: remove + add in single transaction. |
| TC_ORG_066 | Transfer — same team (self-transfer). | 1. Login as LEO.<br>2. Try to transfer member to the same team. | Error "Không thể chuyển trong cùng team" is displayed. | - Source team = target team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `CurrentTeamId == NewTeamId`. |
| TC_ORG_067 | Transfer — source team not found. | 1. Login as LEO.<br>2. Transfer with non-existent source team. | Error 404 "Không tìm thấy team" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks both teams exist. |
| TC_ORG_068 | Transfer — target team not in LEO's office. | 1. Login as LEO of Ward A.<br>2. Transfer member to team in Ward B. | Error "Team không thuộc office" is displayed. | - Target team in different ward. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `newTeam.LocalOfficeId != leoOfficeId`. |
| TC_ORG_069 | Transfer — role mismatch (Cleaner → Inspection). | 1. Login as LEO.<br>2. Transfer Cleaner to Inspection team. | Error "Role không khớp team" is displayed. | - Cleaner role, Inspection team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates `(user.Role, newTeam.TeamType)` compatibility. |
| TC_ORG_070 | Transfer — member not in source team. | 1. Login as LEO.<br>2. Transfer user who is not in the source team. | Error "Thành viên không thuộc team này" is displayed. | - User not in specified team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks existing membership in old team. |
| TC_ORG_071 | Transfer — already in target team. | 1. Login as LEO.<br>2. Transfer user who is already in the target team. | Error 409 "User đã trong team đích" is displayed. | - User already in target team. | Passed | 04/09/2026 | TamKnm | | | | | | | `teamMembers.IsUserInTeamAsync` check. |
| TC_ORG_072 | Transfer — source team has active tasks. | 1. Login as LEO.<br>2. Transfer member when source team has InProgress tasks. | Error "Không thể thay đổi team đang có nhiệm vụ" is displayed. | - Source team has active tasks. | Passed | 04/09/2026 | TamKnm | | | | | | | `TeamMembershipRules.HasActiveTasksAsync` check. |
| TC_ORG_073 | Transfer — as leader when target has leader. | 1. Login as LEO.<br>2. Transfer member as leader to team that already has leader. | Error "Team đích đã có leader" is displayed. | - Target team already has leader. | Passed | 04/09/2026 | TamKnm | | | | | | | `TeamMembershipRules.TeamHasLeaderAsync` for new team. |
| TC_ORG_074 | Transfer — LEO has no office. | 1. Login as LEO without assigned office.<br>2. Try to transfer member. | Error "LEO chưa được gán office" is displayed. | - LEO has no office. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `leo.LocalOfficeId.HasValue`. |

---

## Company Team CRUD (CompanyManager)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_075 | List company teams — success. | 1. Login as CompanyManager.<br>2. Navigate to "Quản lý đội công ty". | List of teams belonging to CM's company. Supports active/inactive filter. | - User is CompanyManager with company. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/teams/company-teams`. |
| TC_ORG_076 | List company teams — filter active only. | 1. Login as CompanyManager.<br>2. Filter by isActive=true. | Only active teams shown. | - User is CompanyManager. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `isActive=true`. |
| TC_ORG_077 | List company teams — Citizen cannot access. | 1. Login as Citizen.<br>2. Attempt to access company teams. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "CompanyManager,Admin")]`. |
| TC_ORG_078 | Create company team — success. | 1. Login as CompanyManager.<br>2. Click "Tạo team".<br>3. Nhập tên "Đội vệ sinh công ty 1".<br>4. Xác nhận. | Success "Đã tạo team công ty thành công." Team created as Cleanup type. CompanyId auto-attached. | - User is CompanyManager. | Passed | 04/09/2026 | TamKnm | | | | | | | Only Cleanup teams allowed for companies. No LocalOffice. |
| TC_ORG_079 | Create company team — empty name. | 1. Login as CompanyManager.<br>2. Để trống tên team.<br>3. Xác nhận. | Error "Tên team là bắt buộc." is displayed. | - User is CompanyManager. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()`. |
| TC_ORG_080 | Create company team — name too long (> 100). | 1. Login as CompanyManager.<br>2. Nhập tên > 100 ký tự. | Error indicating max length exceeded is displayed. | - User is CompanyManager. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(100)`. |
| TC_ORG_081 | Create company team — not a CM. | 1. Login as CompanyStaff (not manager).<br>2. Try to create team. | 403 Forbidden is returned. | - User is CompanyStaff. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "CompanyManager,Admin")]`. |
| TC_ORG_082 | Update company team name — success. | 1. Login as CompanyManager.<br>2. Mở team detail.<br>3. Đổi tên.<br>4. Click "Lưu". | Success "Đã cập nhật team." Name updated. | - Team belongs to CM's company. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `PUT /v1/teams/company-teams/{id}`. |
| TC_ORG_083 | Update company team — team not found. | 1. Login as CompanyManager.<br>2. Update non-existent team. | Error 404 "Không tìm thấy team" is displayed. | - User is CompanyManager. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks team exists. |
| TC_ORG_084 | Update company team — team from another company. | 1. Login as CompanyManager of Company A.<br>2. Try to update team from Company B. | Error 403 "Team không thuộc công ty của bạn" is displayed. | - Team belongs to different company. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `team.CompanyId != staff.CompanyId`. |
| TC_ORG_085 | Toggle company team — deactivate. | 1. Login as CompanyManager.<br>2. Click "Vô hiệu hóa" trên team đang active. | Success "Đã thay đổi trạng thái team." Team deactivated. No new tasks assigned. | - Team is active. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `team.Deactivate()`. |
| TC_ORG_086 | Toggle company team — activate. | 1. Login as CompanyManager.<br>2. Click "Kích hoạt" trên team bị vô hiệu hóa. | Success. Team activated. Can receive new tasks. | - Team is inactive. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `team.Activate()`. |
| TC_ORG_087 | Toggle company team — team from another company. | 1. Login as CompanyManager of Company A.<br>2. Try to toggle team from Company B. | Error "Team không thuộc công ty" is displayed. | - Team belongs to different company. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `team.CompanyId != staff.CompanyId`. |
| TC_ORG_088 | Delete company team — success (soft delete). | 1. Login as CompanyManager.<br>2. Click "Xóa team".<br>3. Xác nhận. | Success "Đã xóa team." Team soft-deleted. No longer visible in list. | - Team has no active assignments. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `DELETE /v1/teams/company-teams/{id}`. Soft delete via `team.Archive()`. |
| TC_ORG_089 | Delete company team — team has active tasks. | 1. Login as CompanyManager.<br>2. Try to delete team with InProgress tasks. | Error 422 "Team còn nhiệm vụ đang xử lý" is displayed. | - Team has active assignments. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `activeAssignments > 0` → DomainException caught. |
| TC_ORG_090 | Delete company team — already deleted. | 1. Login as CompanyManager.<br>2. Try to delete team that is already soft-deleted. | Error 409 "Team đã bị xóa" is displayed. | - Team already deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `team.IsDeleted`. |
| TC_ORG_091 | Delete company team — not a company team. | 1. Login as CompanyManager.<br>2. Try to delete a community team (no CompanyId). | Error "Team không thuộc công ty" is displayed. | - Team is community team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `!team.IsCompanyTeam`. |

---

## Company Team Members (CompanyManager)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_092 | Add company staff to team — success. | 1. Login as CompanyManager.<br>2. Mở team công ty.<br>3. Click "Thêm nhân viên".<br>4. Chọn CompanyStaff thuộc cùng công ty.<br>5. Xác nhận. | Success "Đã thêm nhân viên vào team." Member appears in list. | - User is CompanyManager.<br>- Staff is CompanyStaff in same company. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates staff belongs to same company. |
| TC_ORG_093 | Add company staff — user from different company. | 1. Login as CompanyManager of Company A.<br>2. Try to add staff from Company B. | Error 422 "User không thuộc công ty" is displayed. | - Staff from different company. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates company match. |
| TC_ORG_094 | Add company staff — already in team (duplicate). | 1. Login as CompanyManager.<br>2. Try to add staff who is already in the team. | Error 409 "User đã trong team" is displayed. | - Staff already member. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks existing membership. |
| TC_ORG_095 | Add company staff — team not found. | 1. Login as CompanyManager.<br>2. Try to add staff to non-existent team. | Error 404 "Team không tồn tại" is displayed. | - User is CompanyManager. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks team exists. |
| TC_ORG_096 | Remove company staff from team — success. | 1. Login as CompanyManager.<br>2. Mở team công ty.<br>3. Click "Xóa" bên cạnh nhân viên.<br>4. Xác nhận. | Success "Đã xóa nhân viên khỏi team." Staff record remains (not deleted from company). | - Member exists in team. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `DELETE /v1/teams/company-teams/{teamId}/members/{userId}`. |
| TC_ORG_097 | Remove company staff — member not found. | 1. Login as CompanyManager.<br>2. Try to remove non-existent member. | Error 404 "Không tìm thấy thành viên" is displayed. | - User is CompanyManager. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks member exists. |

---

## Authorization Edge Cases

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_ORG_098 | CompanyManager cannot create Inspection team. | 1. Login as CompanyManager.<br>2. Try to create team with teamType=Inspection (manipulate request). | Handler ignores TeamType from request and always creates Cleanup team. No error but team is Cleanup. | - User is CompanyManager. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: Handler hardcodes `TeamType.Cleanup` but does not return error when client sends Inspection type. Client might think they created Inspection team while it's actually Cleanup. Should either reject the request with clear error or include TeamType in validator. Currently fails silently. |

# Test Report — Inspections

|                      |                              |
| -------------------- | ---------------------------- |
| **Feature**          | **Inspections**              |
| **Test requirement** |                              |
| **Number of TCs**    | **108**                      |

| Testing Round | Passed | Failed | Pending | N/A |
| ------------- | ------ | ------ | ------- | --- |
| **Round 1**   | 106    | 2      | 0       | 0   |
| **Round 2**   | 0      | 0      | 0       | 0   |
| **Round 3**   | 0      | 0      | 0       | 0   |

---

## Get Inspector Queue

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_001 | View inspector queue — success. | 1. Login as Inspector.<br>2. Navigate to "Hàng đợi hồ sơ". | List of inspection reports assigned to Inspector's team is displayed, sorted by newest first. | - User is Inspector.<br>- Team has assigned inspections. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/inspections/queue` with default page=1, pageSize=20. |
| TC_INS_002 | View inspector queue — filter by status. | 1. Login as Inspector.<br>2. Navigate to "Hàng đợi hồ sơ".<br>3. Select filter "InProgress". | Only inspections with InProgress status are shown. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `status=InProgress` filters correctly. |
| TC_INS_003 | View inspector queue — pagination. | 1. Login as Inspector.<br>2. Navigate to "Hàng đợi hồ sơ" page 2 with pageSize 5. | Second page of results is shown with maximum 5 items per page. | - User is Inspector.<br>- Team has >5 inspections. | Passed | 04/09/2026 | TamKnm | | | | | | | Pagination handled by `GetInspectionQueueQuery(page, pageSize, status)`. |
| TC_INS_004 | View inspector queue — unauthorized role (Citizen). | 1. Login as Citizen.<br>2. Attempt to access inspector queue. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "Inspector,Admin")]` blocks Citizen role. |
| TC_INS_005 | View inspector queue — not logged in. | 1. Access inspector queue without JWT token. | 401 Unauthorized is returned. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize]` attribute requires authentication. |

---

## Get Officer (LEO/DEO) Inspection Queue

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_006 | View officer inspection queue — success. | 1. Login as LEO.<br>2. Navigate to "Hàng đợi xử phạt". | List of inspection reports in LEO's ward/office is displayed. Default sort: newest first. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/inspections/officer-queue` with comprehensive filters. |
| TC_INS_007 | Filter by assigned team. | 1. Login as LEO.<br>2. Select a specific team in the team filter. | Only inspections assigned to the selected team are shown. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `assignedTeamId` filters correctly. |
| TC_INS_008 | Filter unassigned inspections only. | 1. Login as LEO.<br>2. Toggle "Chưa gán team" filter. | Only inspections without team assignment are shown. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `unassignedOnly=true`. |
| TC_INS_009 | Filter SLA breached inspections. | 1. Login as LEO.<br>2. Toggle "Vi phạm SLA" filter. | Only SLA-breached inspections are shown. | - User is LEO.<br>- Some inspections have SLA breach. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `slaBreached=true`. |
| TC_INS_010 | Filter by date range. | 1. Login as LEO.<br>2. Set fromDate and toDate filters. | Only inspections created within the date range are shown. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameters `fromDate` and `toDate`. |
| TC_INS_011 | Search by report code or address. | 1. Login as LEO.<br>2. Enter a report code in search box. | Matching inspections are shown. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `search` matches report code, address, violator name. |
| TC_INS_012 | Sort by different fields. | 1. Login as LEO.<br>2. Sort by status ascending. | Inspections are sorted by status in ascending order. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameters `sortBy` and `sortDir`. |
| TC_INS_013 | Unauthorized role (Citizen) cannot access. | 1. Login as Citizen.<br>2. Attempt to access officer inspection queue. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO,DEO,Admin")]` blocks Citizen. |

---

## View Inspection Detail

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_014 | View inspection detail — Inspector (assigned team member). | 1. Login as Inspector in assigned team.<br>2. Click on an inspection in the queue. | Inspection detail page shows: violation info, penalty amount, SLA, payment status, GPS coordinates. | - User is Inspector in the assigned team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates team membership via `ValidateInspectionReadAccessAsync`. |
| TC_INS_015 | View inspection detail — LEO. | 1. Login as LEO.<br>2. Click on an inspection in the officer queue. | Inspection detail page shows full information. | - User is LEO for the ward. | Passed | 04/09/2026 | TamKnm | | | | | | | LEO access validated by ward/office matching. |
| TC_INS_016 | View inspection detail — Admin. | 1. Login as Admin.<br>2. Navigate to any inspection. | Inspection detail page shows full information. | - User is Admin. | Passed | 04/09/2026 | TamKnm | | | | | | | Admin bypasses all team/ward checks. |
| TC_INS_017 | View inspection detail — not found. | 1. Login as Inspector.<br>2. Navigate to an inspection with non-existent ID. | Error 404 "Không tìm thấy hồ sơ" is displayed. | - User is logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection is null` → `Errors.Inspections.InspectionNotFound`. |
| TC_INS_018 | View inspection detail — Inspector not in assigned team. | 1. Login as Inspector in Team A.<br>2. Try to view inspection assigned to Team B. | 403 Forbidden — "Bạn không thuộc team được gán". | - User is Inspector in different team. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamMemberAsync` checks `inspection.AssignedTeamId != member.TeamId`. |

---

## Assign Inspection Team

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_019 | Assign inspection team — success. | 1. Login as LEO.<br>2. Open an unassigned inspection.<br>3. Click "Gán team".<br>4. Select an Inspection Team.<br>5. Confirm. | Success message "Đã gán Inspector Team thành công." Team is displayed on the inspection. If parent Report was Verified, it transitions to InProgress. | - Inspection is in Draft status.<br>- Team is Inspection type with members. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates team exists, is Inspection type, has members, then calls `inspection.AssignTeam(teamId)`. |
| TC_INS_020 | Assign team — inspection not found. | 1. Login as LEO.<br>2. Try to assign team to non-existent inspection. | Error 404 "Không tìm thấy hồ sơ" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection is null`. |
| TC_INS_021 | Assign team — team not found. | 1. Login as LEO.<br>2. Open an inspection.<br>3. Try to assign a non-existent team ID. | Error "Không tìm thấy team" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `team is null`. |
| TC_INS_022 | Assign team — team is not Inspection type. | 1. Login as LEO.<br>2. Open an inspection.<br>3. Try to assign a Cleanup team (not Inspection type). | Error "Team không phải loại Inspection" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `team.TeamType != TeamType.Inspection`. |
| TC_INS_023 | Assign team — team has no members. | 1. Login as LEO.<br>2. Open an inspection.<br>3. Try to assign an empty team (no members). | Error "Team chưa có thành viên" is displayed. | - User is LEO.<br>- Team exists but has no members. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `!await teamMembers.HasMembersAsync(teamId)`. |
| TC_INS_024 | Assign team — wrong status (not Draft/InProgress). | 1. Login as LEO.<br>2. Try to assign team to a Closed inspection. | Error "Trạng thái hồ sơ không cho phép" is displayed. | - Inspection is Closed. | Passed | 04/09/2026 | TamKnm | | | | | | | Domain method `inspection.AssignTeam()` validates status. |
| TC_INS_025 | Assign team — unauthorized role (Citizen). | 1. Login as Citizen.<br>2. Attempt to assign team to an inspection. | 403 Forbidden is returned. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO,Admin")]`. |
| TC_INS_026 | Assign team — notification sent to team. | 1. Login as LEO.<br>2. Assign a team to an inspection. | Team members receive notification about the new assignment. | - User is LEO.<br>- Team has members. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `taskAssignedNotifier.NotifyTeamAsync(...)`. |

---

## Accept Inspection Task

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_027 | Accept inspection task — success. | 1. Login as Inspector (team member).<br>2. Open assigned inspection (Draft).<br>3. Click "Nhận task". | Success message "Đã nhận task điều tra." Inspection status changes Draft → InProgress. | - Inspection is Draft, assigned to user's team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates team membership, calls `inspection.AcceptTask(currentUser.UserId)`. |
| TC_INS_028 | Accept task — not assigned to your team. | 1. Login as Inspector in Team A.<br>2. Try to accept task assigned to Team B. | Error "Bạn không thuộc team được gán" is displayed. | - User is Inspector in different team. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamMemberAsync` checks team membership. |
| TC_INS_029 | Accept task — inspection not found. | 1. Login as Inspector.<br>2. Try to accept a non-existent inspection ID. | Error 404 "Không tìm thấy hồ sơ" is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection is null`. |
| TC_INS_030 | Accept task — wrong status. | 1. Login as Inspector.<br>2. Try to accept an already InProgress inspection. | Error indicating invalid status transition is displayed. | - Inspection is already InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Domain method `AcceptTask()` validates status. |
| TC_INS_031 | Accept task — idempotency key. | 1. Login as Inspector.<br>2. Click "Nhận task" twice rapidly. | Only one transition occurs. Second call returns success without error. | - Inspection is Draft. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller has `[SupportsIdempotency]` attribute. |

---

## Confirm Arrival

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_032 | Confirm arrival — within 200m. | 1. Login as Inspector.<br>2. Open InProgress inspection.<br>3. App auto-detects GPS within 200m of report location.<br>4. Click "Xác nhận có mặt". | Success message "Đã xác nhận có mặt hiện trường." is displayed. | - Inspection is InProgress.<br>- Inspector is within 200m. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks GPS distance, ≤200m passes without note. |
| TC_INS_033 | Confirm arrival — beyond 200m without note. | 1. Login as Inspector.<br>2. Open InProgress inspection.<br>3. GPS is >200m from report location.<br>4. Click "Xác nhận" without entering note. | Error message "Vui lòng ghi chú giải trình khi ở xa hiện trường." is displayed. | - Inspector is >200m from report. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `distance > SoftGpsThresholdMeters && string.IsNullOrWhiteSpace(request.Note)`. |
| TC_INS_034 | Confirm arrival — beyond 200m with note. | 1. Login as Inspector.<br>2. GPS is >200m from report location.<br>3. Enter explanatory note in the text field.<br>4. Click "Xác nhận". | Success. Arrival confirmed with note recorded. | - Inspector is >200m from report. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler allows >200m when note is provided. |
| TC_INS_035 | Confirm arrival — not in assigned team. | 1. Login as Inspector in different team.<br>2. Try to confirm arrival for another team's inspection. | Error "Bạn không thuộc team được gán" is displayed. | - User not in assigned team. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamMemberAsync` checks team membership. |
| TC_INS_036 | Confirm arrival — inspection not found. | 1. Login as Inspector.<br>2. Try to confirm arrival for non-existent inspection. | Error 404 "Không tìm thấy hồ sơ" is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection is null`. |
| TC_INS_037 | Confirm arrival — report not found. | 1. Login as Inspector.<br>2. Try to confirm arrival when parent report is deleted. | Error 404 "Không tìm thấy báo cáo" is displayed. | - Parent report is deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `report is null` → `Errors.Reports.ReportNotFound`. |

---

## Update Inspection Checklist

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_038 | Update checklist text — success. | 1. Login as Inspector (team member).<br>2. Open InProgress inspection.<br>3. Fill "Tình trạng vi phạm" and "Ghi chú khác".<br>4. Click "Lưu". | Success message "Đã cập nhật checklist." is displayed. | - Inspection is InProgress.<br>- User is team member. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler delegates to `UpdateInspectionChecklistCommand`. |
| TC_INS_039 | Update checklist — not in assigned team. | 1. Login as Inspector in different team.<br>2. Try to update checklist. | Error "Bạn không thuộc team được gán" is displayed. | - User not in assigned team. | Passed | 04/09/2026 | TamKnm | | | | | | | Authorization validated before update. |

---

## Upload Inspection Evidence

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_040 | Upload scene photos — success (≥2 photos). | 1. Login as Inspector (team member).<br>2. Open InProgress inspection.<br>3. Upload 2 scene photos via presigned URLs.<br>4. Submit evidence JSON. | Success. Evidence saved. Total count returned. | - Inspection is InProgress.<br>- Field report not yet submitted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler saves evidence items, validates URLs against R2 storage. |
| TC_INS_041 | Upload evidence — no items (empty array). | 1. Login as Inspector.<br>2. Submit evidence with empty items array. | Error "At least one evidence item is required." is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `request.Items.Count == 0` → `Errors.Inspections.EvidenceImagesRequired`. |
| TC_INS_042 | Upload evidence — field report already submitted. | 1. Login as Inspector.<br>2. Try to upload evidence after field report is submitted. | Error "Biên bản đã nộp, không thể thay đổi" is displayed. | - Field investigation already submitted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection.FieldInvestigationSubmittedAt.HasValue`. |
| TC_INS_043 | Upload evidence — inspection not InProgress. | 1. Login as Inspector.<br>2. Try to upload evidence for a Draft inspection. | Error "Trạng thái không hợp lệ" is displayed. | - Inspection is Draft. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection.Status != InspectionStatus.InProgress`. |
| TC_INS_044 | Upload evidence — invalid storage URL. | 1. Login as Inspector.<br>2. Submit evidence with a URL not from R2 storage. | Error "URL không hợp lệ" is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates `fileStorage.IsOwnedPublicUrl(url)`. |
| TC_INS_045 | Upload evidence — URL outside expected folder. | 1. Login as Inspector.<br>2. Submit evidence URL pointing to wrong folder path. | Error "URL không hợp lệ" is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates `InspectionEvidenceUploadRules.UrlMatchesFolder(url, folderPrefix)`. |
| TC_INS_046 | Upload evidence — exceeds max items per request. | 1. Login as Inspector.<br>2. Submit more items than allowed per request. | Error "Maximum N items per request." is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator checks `items.Count <= MaxItemsPerRequest`. |
| TC_INS_047 | Upload evidence — file exceeds size limit. | 1. Login as Inspector.<br>2. Submit evidence item with SizeBytes exceeding category limit. | Error "File exceeds size limit for {Category}." is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator custom rule checks `MaxBytesFor(category)`. |
| TC_INS_048 | Upload evidence — ViolationStatus category rejected. | 1. Login as Inspector.<br>2. Submit evidence with category = ViolationStatus. | Error "ViolationStatus is text-only — use PUT /checklist." is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator rejects `InspectionEvidenceCategory.ViolationStatus`. |
| TC_INS_049 | Upload evidence — not in assigned team. | 1. Login as Inspector in different team.<br>2. Try to upload evidence. | Error "Bạn không thuộc team được gán" is displayed. | - User not in assigned team. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamMemberAsync` checks team membership. |

---

## Submit Field Investigation

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_050 | Submit field investigation — success. | 1. Login as Inspector Team Leader.<br>2. Open InProgress inspection with ≥2 scene photos.<br>3. Click "Nộp biên bản điều tra". | Success message "Đã nộp biên bản điều tra hiện trường." Field report is locked. | - User is Team Leader.<br>- ≥2 ScenePhoto evidence items exist. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates team leader, checklist completion, calls `inspection.SubmitFieldInvestigation(userId)`. |
| TC_INS_051 | Submit field investigation — not team leader. | 1. Login as regular Inspector (not leader).<br>2. Try to submit field investigation. | Error "Bạn không phải Team Leader" is displayed. | - User is team member but not leader. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamLeaderAsync` checks leadership role. |
| TC_INS_052 | Submit field investigation — insufficient evidence. | 1. Login as Inspector Team Leader.<br>2. Try to submit field report with < 2 scene photos. | Error "Cần ít nhất 2 ảnh hiện trường" is displayed. | - User is Team Leader.<br>- Less than 2 scene photos. | Passed | 04/09/2026 | TamKnm | | | | | | | `InspectionChecklistValidator.Validate(items)` checks scene photo count. |
| TC_INS_053 | Submit field investigation — inspection not found. | 1. Login as Inspector.<br>2. Try to submit field report for non-existent inspection. | Error 404 "Không tìm thấy hồ sơ" is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection is null`. |

---

## Update Inspection Details

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_054 | Update inspection details — success. | 1. Login as Inspector.<br>2. Open Draft inspection.<br>3. Fill in violation description, violator name, address, identity.<br>4. Click "Lưu". | Success message "Đã cập nhật biên bản hiện trường." Details are updated. | - Inspection is in Draft status.<br>- User is Inspector or Admin. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler delegates to `UpdateInspectionDetailsCommand`. |
| TC_INS_055 | Update details — wrong status (not Draft). | 1. Login as Inspector.<br>2. Try to update details of an InProgress inspection. | Error "Trạng thái không hợp lệ — chỉ cập nhật khi Draft" is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller description says "Chỉ có thể cập nhật khi hồ sơ ở trạng thái Draft". |
| TC_INS_056 | Update details — link violating entity. | 1. Login as Inspector.<br>2. Open Draft inspection.<br>3. Select existing violating entity from list. | Details updated with ViolatingEntityId linked. | - Violating entity exists in system. | Passed | 04/09/2026 | TamKnm | | | | | | | Request includes optional `ViolatingEntityId` field. |

---

## Issue Penalty

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_057 | Issue penalty — success. | 1. Login as Inspector Team Leader.<br>2. Open investigation-completed inspection with ≥2 scene photos.<br>3. Click "Ban hành QĐ xử phạt".<br>4. Fill in: Violation Level, Penalty Amount, Decision Number, Payment Due Days.<br>5. Confirm. | Success message "Đã ban hành quyết định xử phạt." Status changes to PenaltyIssued. | - User is Team Leader.<br>- Checklist complete (≥2 scene photos).<br>- Field investigation submitted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates leader, checklist, calls `inspection.IssuePenalty(...)`. |
| TC_INS_058 | Issue penalty — not team leader. | 1. Login as regular Inspector.<br>2. Try to issue penalty. | Error "Bạn không phải Team Leader" is displayed. | - User is team member but not leader. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamLeaderAsync` rejects non-leaders. |
| TC_INS_059 | Issue penalty — insufficient evidence (< 2 photos). | 1. Login as Team Leader.<br>2. Try to issue penalty with <2 scene photos. | Error "Cần ít nhất 2 ảnh hiện trường" is displayed. | - Less than 2 scene photos. | Passed | 04/09/2026 | TamKnm | | | | | | | `InspectionChecklistValidator.Validate(evidenceItems)` checks. |
| TC_INS_060 | Issue penalty — penalty amount = 0 (invalid). | 1. Login as Team Leader.<br>2. Enter penalty amount = 0.<br>3. Confirm. | Error "Số tiền phạt phải lớn hơn 0." is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `GreaterThan(0)` for PenaltyAmount. |
| TC_INS_061 | Issue penalty — empty decision number. | 1. Login as Team Leader.<br>2. Leave "Số quyết định" empty.<br>3. Confirm. | Error indicating decision number is required is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for DecisionNumber. |
| TC_INS_062 | Issue penalty — decision number too long (> 50 chars). | 1. Login as Team Leader.<br>2. Enter decision number longer than 50 characters.<br>3. Confirm. | Error indicating maximum length exceeded is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(50)` for DecisionNumber. |
| TC_INS_063 | Issue penalty — payment due days out of range. | 1. Login as Team Leader.<br>2. Enter payment due days = 0 or > 90.<br>3. Confirm. | Error indicating due days must be between 1-90 is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `InclusiveBetween(1, 90)` for PaymentDueDays. |
| TC_INS_064 | Issue penalty — invalid violation level. | 1. Login as Team Leader.<br>2. Submit with invalid ViolationLevel enum value.<br>3. Confirm. | Error indicating invalid violation level is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `IsInEnum()` for ViolationLevel. |
| TC_INS_065 | Issue penalty — repeat offender detected (ViolatingEntityId). | 1. Login as Team Leader.<br>2. Issue penalty for a violator with 1+ previous penalties in 12 months (linked via ViolatingEntityId). | Penalty issued with repeat offender flag = true. | - Violating entity has ≥1 previous inspection in 12 months. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `violatingEntities.CountInspectionsInPeriodAsync(entityId, 12, ct)`. |
| TC_INS_066 | Issue penalty — repeat offender fallback (ViolatorIdentity string). | 1. Login as Team Leader.<br>2. Issue penalty for violator without ViolatingEntityId but with matching identity string. | Penalty issued with repeat offender flag set based on string match. | - No ViolatingEntityId but ViolatorIdentity matches previous records. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler falls back to `inspections.CountByViolatorInPeriodAsync(identity, 12)`. |
| TC_INS_067 | Issue penalty — additional measures too long. | 1. Login as Team Leader.<br>2. Enter additional measures exceeding 1000 characters.<br>3. Confirm. | Error indicating max length exceeded is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MaximumLength(1000)` for AdditionalMeasures. |
| TC_INS_068 | Issue penalty — inspection not found. | 1. Login as Team Leader.<br>2. Try to issue penalty for non-existent inspection. | Error 404 "Không tìm thấy hồ sơ" is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection is null`. |

---

## Close No Violation

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_069 | Close no violation — success. | 1. Login as Inspector Team Leader.<br>2. Open investigation-completed inspection.<br>3. Click "Đóng — không vi phạm".<br>4. Enter reason (≥50 characters).<br>5. Confirm. | Success message "Đã đóng hồ sơ — không đủ căn cứ vi phạm." Status changes to ClosedNoViolation. Citizen receives notification. | - User is Team Leader.<br>- Inspection is InProgress or FieldReportSubmitted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates leader, calls `inspection.CloseNoViolation(reason)`, sends notification to reporter. |
| TC_INS_070 | Close no violation — reason too short (< 50 chars). | 1. Login as Team Leader.<br>2. Enter reason with less than 50 characters.<br>3. Confirm. | Error "Lý do đóng hồ sơ phải có ít nhất 50 ký tự" is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MinimumLength(50)` for Reason. |
| TC_INS_071 | Close no violation — empty reason. | 1. Login as Team Leader.<br>2. Leave reason empty.<br>3. Confirm. | Error "Lý do không được để trống" is displayed. | - User is Team Leader. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for Reason. |
| TC_INS_072 | Close no violation — not team leader. | 1. Login as regular Inspector.<br>2. Try to close inspection. | Error "Bạn không phải Team Leader" is displayed. | - User is team member but not leader. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamLeaderAsync` rejects non-leaders. |
| TC_INS_073 | Close no violation — wrong status. | 1. Login as Team Leader.<br>2. Try to close a Closed inspection. | Error "Trạng thái không hợp lệ" is displayed. | - Inspection already Closed. | Passed | 04/09/2026 | TamKnm | | | | | | | Domain method validates status transition. |
| TC_INS_074 | Close no violation — notification to citizen. | 1. Login as Team Leader.<br>2. Close inspection with valid reason. | Citizen who submitted the original report receives a notification about the closure. | - Report has a reporter. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `closedNotifier.NotifyReporterAsync(...)`. |

---

## Decline Inspection

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_075 | Decline inspection — success (within 24h). | 1. Login as Inspector (team member).<br>2. Open newly assigned inspection (Draft, within 24h).<br>3. Click "Từ chối".<br>4. Enter reason (≥10 characters).<br>5. Confirm. | Success message "Đã từ chối hồ sơ xử phạt." Team is cleared. Inspection stays Draft for LEO re-assignment. | - Inspection is Draft, within 24h of creation. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates 24h window, clears team, notifies LEO. |
| TC_INS_076 | Decline — past 24h window. | 1. Login as Inspector.<br>2. Try to decline inspection after 24h from creation. | Error "Đã quá hạn 24 giờ để từ chối" is displayed. | - Inspection created >24h ago. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `(DateTime.UtcNow - inspection.CreatedAt).TotalHours > 24`. |
| TC_INS_077 | Decline — reason too short (< 10 chars). | 1. Login as Inspector.<br>2. Enter reason with less than 10 characters.<br>3. Confirm. | Error "Lý do từ chối phải có ít nhất 10 ký tự." is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `MinimumLength(10)` for Reason. |
| TC_INS_078 | Decline — empty reason. | 1. Login as Inspector.<br>2. Leave reason empty.<br>3. Confirm. | Error "Lý do không được để trống" is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Validator `NotEmpty()` for Reason. |
| TC_INS_079 | Decline — wrong status (not Draft). | 1. Login as Inspector.<br>2. Try to decline an InProgress inspection. | Error "Trạng thái không hợp lệ" is displayed. | - Inspection is InProgress. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection.Status != InspectionStatus.Draft`. |
| TC_INS_080 | Decline — no team assigned. | 1. Login as Inspector.<br>2. Try to decline an inspection with no team. | Error "Hồ sơ chưa có team được gán" is displayed. | - Inspection has no team. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `inspection.AssignedTeamId is null`. |
| TC_INS_081 | Decline — not in assigned team. | 1. Login as Inspector in Team A.<br>2. Try to decline inspection assigned to Team B. | Error "Bạn không thuộc team được gán" is displayed. | - User in different team. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamMemberAsync` checks membership. |
| TC_INS_082 | Decline — LEO notification. | 1. Login as Inspector.<br>2. Decline inspection within 24h. | LEO who created the inspection receives notification about the decline with reason. | - Inspection has `CreatedByOfficerId`. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `declinedNotifier.NotifyLeoAsync(...)`. |

---

## Record Payment

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_083 | Record payment — success (full amount). | 1. Login as LEO.<br>2. Open PenaltyIssued inspection.<br>3. Click "Ghi nhận nộp phạt".<br>4. Enter paid amount matching the remaining balance.<br>5. Upload receipt photo.<br>6. Confirm. | Success message "Đã ghi nhận nộp phạt đủ và đóng hồ sơ." Status transitions: PenaltyIssued → Paid → Closed. Inspector who issued penalty receives notification. | - User is LEO for the ward.<br>- Inspection is PenaltyIssued. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates LEO ward access, records payment, auto-closes, notifies Inspector. |
| TC_INS_084 | Record payment — no receipt uploaded. | 1. Login as LEO.<br>2. Submit payment without uploading receipt. | Error "Vui lòng upload ảnh biên lai nộp phạt." is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller checks `receipt is null || receipt.Length == 0` → 400. |
| TC_INS_085 | Record payment — amount mismatch. | 1. Login as LEO.<br>2. Enter paid amount different from remaining balance. | Error "Số tiền không khớp số còn lại" is displayed. | - Paid amount ≠ remaining balance. | Passed | 04/09/2026 | TamKnm | | | | | | | Domain method `inspection.RecordPayment(payment)` validates amount. |
| TC_INS_086 | Record payment — wrong status (not PenaltyIssued). | 1. Login as LEO.<br>2. Try to record payment for Draft inspection. | Error "Trạng thái không hợp lệ" is displayed. | - Inspection is Draft. | Passed | 04/09/2026 | TamKnm | | | | | | | Domain method validates status allows payment. |
| TC_INS_087 | Record payment — LEO not assigned to report's ward. | 1. Login as LEO from Ward A.<br>2. Try to record payment for inspection in Ward B. | Error "Bạn không phải LEO phụ trách khu vực này" is displayed. | - LEO's ward ≠ report's ward. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateLeoForReportAsync` checks `leoOffice.Id != report.AssignedOfficeId`. |
| TC_INS_088 | Record payment — Inspector cannot record payment. | 1. Login as Inspector.<br>2. Try to record payment. | 403 Forbidden is returned. | - User is Inspector (not LEO). | Passed | 04/09/2026 | TamKnm | | | | | | | `[Authorize(Roles = "LEO,Admin")]` blocks Inspector. |
| TC_INS_089 | Record payment — missing RecordPaymentCommandValidator. | 1. Login as LEO.<br>2. Submit payment with negative amount or future paidAt date. | Payment is processed without FluentValidation input checks. | - User is LEO. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: No `RecordPaymentCommandValidator` exists. Negative amounts and future dates are not validated at the input layer. Domain may catch some but input validation is missing. |

---

## Get Payment History

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_090 | View payment history — success. | 1. Login as Inspector/LEO.<br>2. Open inspection with payments.<br>3. Click "Lịch sử nộp phạt". | Payment history page shows: total penalty, total paid, remaining, and each payment record. | - Inspection has payment records. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/inspections/{id}/payments`. |
| TC_INS_091 | View payment history — inspection not found. | 1. Login as Inspector.<br>2. View payment history for non-existent inspection. | Error 404 is displayed. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks inspection exists. |

---

## Delete Penalty Payment

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_092 | Delete payment — success (soft delete). | 1. Login as LEO/Inspector.<br>2. Open payment history.<br>3. Click "Xóa" on a payment record. | Success message "Đã xóa khoản nộp phạt." Payment is soft-deleted. Inspection's paid amount is recalculated. | - Payment record exists. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler soft-deletes payment, calls `inspection.RemovePayment(payment)`. |
| TC_INS_093 | Delete payment — payment not found. | 1. Login as LEO.<br>2. Try to delete non-existent payment ID. | Error "Không tìm thấy thanh toán" is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `existingPayment is null`. |
| TC_INS_094 | Delete payment — payment already deleted. | 1. Login as LEO.<br>2. Try to delete a payment that is already soft-deleted. | Error "Thanh toán đã bị xóa" is displayed. | - Payment already soft-deleted. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler checks `existingPayment.IsDeleted`. |

---

## Close Inspection

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_095 | Close inspection — success (Paid → Closed). | 1. Login as Inspector Team Leader.<br>2. Open Paid inspection.<br>3. Click "Đóng hồ sơ".<br>4. Optionally enter reason.<br>5. Confirm. | Success message "Đã đóng hồ sơ xử phạt." Status changes to Closed. | - User is Team Leader.<br>- Inspection is Paid. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler validates leader, calls `inspection.Close(reason)`. |
| TC_INS_096 | Close inspection — wrong status (not Paid). | 1. Login as Team Leader.<br>2. Try to close a Draft inspection. | Error "Trạng thái không phải Paid" is displayed. | - Inspection is Draft. | Passed | 04/09/2026 | TamKnm | | | | | | | Domain method validates only Paid can be Closed. |
| TC_INS_097 | Close inspection — not team leader. | 1. Login as regular Inspector.<br>2. Try to close inspection. | Error "Bạn không phải Team Leader" is displayed. | - User is team member but not leader. | Passed | 04/09/2026 | TamKnm | | | | | | | `ValidateTeamLeaderAsync` rejects non-leaders. |
| TC_INS_098 | Close inspection — audit log created. | 1. Login as Team Leader.<br>2. Close a Paid inspection. | Audit log is created with old status (Paid) and new status (Closed). | - Inspection is Paid. | Passed | 04/09/2026 | TamKnm | | | | | | | Handler calls `auditLogger.LogAsync(...)` with status snapshots. |

---

## Deprecated Endpoints

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_099 | Check-in endpoint (deprecated). | 1. Login as Inspector.<br>2. Call POST /v1/inspections/{id}/check-in. | 410 Gone with message "Endpoint deprecated — use POST /accept + POST /confirm-arrival". | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller returns 410 via `DeprecatedInspectionEndpoint()`. |
| TC_INS_100 | Update progress endpoint (deprecated). | 1. Login as Inspector.<br>2. Call PUT /v1/inspections/{id}/progress. | 410 Gone with message "Endpoint deprecated — use checklist workflow". | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Controller returns 410 via `DeprecatedInspectionEndpoint()`. |

---

## KPI Inspection Team

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_101 | View KPI — Inspector views own team. | 1. Login as Inspector.<br>2. Navigate to "KPI" section. | KPI data displayed: penalty on-time rate, payment compliance, repeat offenders, SLA breach. | - User is Inspector with team. | Passed | 04/09/2026 | TamKnm | | | | | | | Endpoint `GET /v1/inspections/kpi` without teamId defaults to user's team. |
| TC_INS_102 | View KPI — LEO views specific team. | 1. Login as LEO.<br>2. Navigate to "KPI" section.<br>3. Select a specific Inspector Team. | KPI data for selected team is displayed. | - User is LEO. | Passed | 04/09/2026 | TamKnm | | | | | | | LEO can pass `teamId` query parameter. |
| TC_INS_103 | View KPI — Admin views any team. | 1. Login as Admin.<br>2. Navigate to "KPI" section.<br>3. Select any team. | KPI data for selected team is displayed. | - User is Admin. | Passed | 04/09/2026 | TamKnm | | | | | | | Admin bypasses team access checks. |
| TC_INS_104 | View KPI — filter by period. | 1. Login as Inspector.<br>2. Select period filter (e.g. Monthly). | KPI data filtered by selected period. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameter `period` supports KpiPeriod enum values. |
| TC_INS_105 | View KPI — filter by date range. | 1. Login as Inspector.<br>2. Set custom from/to dates. | KPI data filtered by custom date range. | - User is Inspector. | Passed | 04/09/2026 | TamKnm | | | | | | | Query parameters `from` and `to`. |

---

## Authorization & Edge Cases

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester | Note |
| ------------ | --------------------- | ------------------- | ---------------- | -------------- | ------- | --------- | ------ | ------- | --------- | ------ | ------- | --------- | ------ | ---- |
| TC_INS_106 | All mutation endpoints require authentication. | 1. Call assign-team, accept, confirm-arrival, checklist, evidence, issue-penalty, close, decline, record-payment without JWT. | All return 401 Unauthorized. | - User is not logged in. | Passed | 04/09/2026 | TamKnm | | | | | | | All endpoints have `[Authorize]` with specific roles. |
| TC_INS_107 | Citizen cannot access any inspection endpoint. | 1. Login as Citizen.<br>2. Try to access queue, detail, or any mutation endpoint. | 403 Forbidden for all endpoints. | - User is Citizen. | Passed | 04/09/2026 | TamKnm | | | | | | | All endpoints restricted to Inspector, LEO, DEO, or Admin roles. |
| TC_INS_108 | Legacy evidence-images route (alias). | 1. Login as Inspector.<br>2. Call POST /v1/inspections/{id}/evidence-images with items. | Evidence saved as ScenePhoto category. Response same as POST /evidence. | - Inspection is InProgress. | Failed | 04/09/2026 | TamKnm | | | | | | | BUG: The `evidence-images` route is marked `[Obsolete]` but still functional. It bypasses category selection by hardcoding `ScenePhoto`. While this works, the `[Obsolete]` attribute generates compiler warnings and the endpoint should either be fully removed or the deprecation warning should be communicated to FE consumers. |

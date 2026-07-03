        # Report Lifecycle — GreenLens (SU26SE049)

> Luồng xử lý báo cáo ô nhiễm từ khi Citizen gửi đến khi đóng case, bao gồm tất cả actor, API endpoint, business rule, và SLA.

---

## Mục lục

1. [State Machine](#1-state-machine)
2. [Luồng theo từng Actor](#2-luồng-theo-từng-actor)
   - [Citizen](#21-citizen)
   - [Environmental Officer (LEO / DEO)](#22-environmental-officer-leo--deo)
   - [Company Manager (CM)](#23-company-manager-cm)
   - [Cleanup Team](#24-cleanup-team)
   - [Inspection Sub-flow (song song)](#25-inspection-sub-flow-song-song)
3. [Toàn bộ API Endpoints](#3-toàn-bộ-api-endpoints)
4. [SLA Reference](#4-sla-reference)
5. [Background Jobs liên quan](#5-background-jobs-liên-quan)

---

## 1. State Machine

> **BR-REP-020 / BR-REP-021** — Transition chỉ được thực hiện qua method của Domain entity, không set `Status` trực tiếp.

### Happy Path

```
Submitted ──► Verified ──► InProgress ──► Resolved ──► Closed
```

### Nhánh rẽ

```
Submitted ──► Rejected    (Officer, reason ≥ 20 ký tự — BR-REP-022)
Submitted ──► Duplicate   (Officer hoặc AI — BR-REP-030)
Verified  ──► Duplicate   (Officer hoặc AI)
Resolved  ──► InProgress  (Citizen reopen, tối đa 2 lần — BR-REP-015)
```

### Bảng chuyển trạng thái đầy đủ

| Từ trạng thái | Sang trạng thái | Ai thực hiện | Method Domain | BR |
|---|---|---|---|---|
| *(tạo mới)* | `Submitted` | Citizen | `Report.Create()` | BR-REP-001, 003, 005, 010, 013 |
| `Submitted` | `Verified` | Officer / Admin | `Report.Verify()` | BR-REP-020, BR-OFF-004 |
| `Submitted` | `Rejected` | Officer / Admin | `Report.Reject()` | BR-REP-021, BR-REP-022 |
| `Submitted` | `Duplicate` | Officer / AI | `Report.MarkDuplicate()` | BR-REP-030 |
| `Verified` | `Duplicate` | Officer / AI | `Report.MarkDuplicate()` | BR-REP-030 |
| `Verified` | `InProgress` | Officer (assign gov team) | `Report.Assign()` | BR-OFF-011 |
| `Verified` | `InProgress` | CM (assign company team) | `Report.AssignByCompanyManager()` | BR-CMP-010 |
| `InProgress` | `Resolved` | Cleanup Team | `Report.Resolve()` | BR-CLN-005, BR-REP-014 |
| `Resolved` | `Closed` | Citizen / Auto / Admin | `Report.Close()` | BR-REP-016, BR-REP-025 |
| `Resolved` | `InProgress` | Citizen (reopen) | `Report.TryReopen()` | BR-REP-015 |

---

## 2. Luồng theo từng Actor

---

### 2.1 Citizen

**Vai trò:** Gửi báo cáo, theo dõi tiến độ, xác nhận hoặc tranh chấp kết quả.

#### Bước 1 — AI Pre-analysis *(tùy chọn)*

- Upload ảnh lên endpoint phân tích trước khi submit
- AI trả về loại ô nhiễm, mức độ nghiêm trọng ước tính, cờ gian lận
- Kết quả cache 15 phút, dùng khi tạo report thật

```
POST /v1/reports/analyze
```

| | |
|---|---|
| **BR** | BR-AI-001, BR-AI-006 (timeout 5s → fallback queue) |
| **Roles** | Citizen |

---

#### Bước 2 — Submit Report

- GPS bắt buộc, phải trong bounds Việt Nam: lat 8.0–24.0, lng 102.0–110.0
- Tối thiểu 1 ảnh, phải có danh mục
- Rate limit: 5 báo cáo/giờ, 20/ngày (Redis sliding window)
- Sau khi submit: tự động route tới đơn vị phụ trách theo ward (BR-ORG-010/011)
- Trạng thái ban đầu: **`Submitted`**

```
POST /v1/reports
```

| | |
|---|---|
| **BR** | BR-REP-001, BR-REP-003, BR-REP-005, BR-REP-010, BR-REP-013, BR-ORG-010, BR-ORG-011 |
| **Roles** | Citizen |

---

#### Bước 3 — Theo dõi trạng thái

```
GET /v1/reports/my           # Danh sách báo cáo của bản thân
GET /v1/reports/{id}         # Chi tiết đầy đủ + assignments + media + history
GET /v1/reports/{id}/history # Timeline các lần đổi trạng thái
```

| | |
|---|---|
| **Roles** | Citizen (my), All (by id) |

---

#### Bước 4 — Xác nhận hoặc Tranh chấp

Khi report ở trạng thái **`Resolved`**:

- **Đồng ý** → đóng case
- **Tranh chấp** → reopen, tối đa 2 lần
- Nếu không hành động sau **7 ngày** → `AutoCloseResolvedReportJob` tự đóng

```
PUT /v1/reports/{id}/close   # Resolved → Closed
PUT /v1/reports/{id}/reopen  # Resolved → InProgress (max 2×)
```

| | |
|---|---|
| **BR** | BR-REP-015 (max 2 reopen), BR-REP-016 (Closed là final), BR-REP-025 (auto-close 7 ngày) |
| **Roles** | Citizen (reopen), All (close) |

---

### 2.2 Environmental Officer (LEO / DEO)

**Vai trò:** Xác minh báo cáo, phân công cleanup, quản lý SLA, dispatch cho công ty.

#### Bước 1 — Xem hàng đợi

- Hiển thị báo cáo theo priority score: `severity×3 + relatedCount×2 + ageInHours/24`
- Filter theo: status, severity, ward, category, date range

```
GET /v1/reports/queue
GET /v1/reports/{id}
```

| | |
|---|---|
| **BR** | BR-OFF-010 (priority scoring) |
| **Roles** | Officer, DEO, Admin |

---

#### Bước 2 — Xác minh hoặc Từ chối

- **Không thể** xác minh báo cáo của chính mình (BR-OFF-004)
- Có thể override severity và category khi verify
- Từ chối: bắt buộc lý do ≥ 20 ký tự
- Khi verify: SLA deadline được tính toán và lưu

```
PUT /v1/reports/{id}/verify  # Submitted → Verified
PUT /v1/reports/{id}/reject  # Submitted → Rejected
```

| | |
|---|---|
| **BR** | BR-OFF-004 (segregation of duties), BR-OFF-002 (SLA verify 24h), BR-REP-022 (reason ≥ 20 chars) |
| **Roles** | Officer, Admin |

---

#### Bước 3a — Gắn nhãn loại rác *(có thể làm bất kỳ lúc nào)*

Có thể thực hiện ở Submitted, Verified, hoặc InProgress.

```
GET /v1/waste-tags            # Danh sách nhãn
PUT /v1/reports/{id}/waste-tags  # Gắn/cập nhật nhãn cho report
```

| | |
|---|---|
| **Roles** | Officer, Admin |

---

#### Bước 3b — Phân công Cleanup Team (gov)

- Có thể assign nhiều team cùng lúc
- Mỗi team tối đa 1 task `InProgress` tại một thời điểm (BR-OFF-013)
- Report chuyển `InProgress` khi ít nhất 1 team **accept**

```
POST /v1/reports/{id}/assign    # Assigned gov cleanup team(s)
PUT  /v1/reports/{id}/reassign  # Thay thế team (decline cũ → assign mới)
```

| | |
|---|---|
| **BR** | BR-OFF-011 (multi-team), BR-OFF-012 (reassign cùng loại team), BR-OFF-013 (max 1 active/team) |
| **Roles** | Officer, Admin |

---

#### Bước 3c — Dispatch cho Công ty *(thay thế 3b)*

- Công ty phải đang active và phục vụ ward của báo cáo
- Sau khi dispatch, Company Manager sẽ assign team của họ
- Report vẫn ở `Verified`, chờ CM assign

```
POST /v1/reports/{id}/dispatch-to-company
```

| | |
|---|---|
| **BR** | BR-CMP-005 (company active), BR-CMP-008 (company serves ward) |
| **Roles** | Officer, Admin |

---

#### Bước 4 — Theo dõi tiến độ & SLA

- Background job chạy mỗi 30 phút đánh dấu SLA breach
- Officer được notify khi SLA sắp vi phạm

```
GET /v1/reports/{id}/progress      # Chi tiết: team status, %, ảnh, SLA countdown
GET /v1/reports/progress-board     # Bảng tổng hợp tất cả reports + SLA
```

| | |
|---|---|
| **BR** | BR-OFF-020 (SLA resolution), BR-OFF-002 (SLA verify) |
| **Roles** | Officer, Admin |

---

### 2.3 Company Manager (CM)

**Vai trò:** Nhận báo cáo được dispatch từ Officer, phân công team nội bộ.

#### Bước 1 — Xem hàng đợi công ty

Chỉ thấy báo cáo có `Status = Verified` và `CompanyId = công ty mình`.

```
GET /v1/reports/company-queue
```

| | |
|---|---|
| **Roles** | CompanyMgr, Admin |

---

#### Bước 2 — Assign team nội bộ

Report chuyển `Verified → InProgress`.

```
POST /v1/reports/{id}/assign-company-team
```

| | |
|---|---|
| **BR** | BR-CMP-010 |
| **Roles** | CompanyMgr, Admin |

---

#### Bước 3 — Theo dõi tiến độ

```
GET /v1/reports/progress-board
GET /v1/reports/{id}/progress
```

---

### 2.4 Cleanup Team

**Vai trò:** Thực hiện dọn dẹp thực địa, cập nhật tiến độ, upload ảnh trước/sau.

#### Bước 1 — Xem danh sách task

```
GET /v1/teams/my-tasks              # Tất cả task của team
GET /v1/teams/my-tasks/{reportId}   # Chi tiết 1 task
GET /v1/teams/my-profile            # Profile + workload của team
```

| | |
|---|---|
| **Roles** | CleanupTeam |

---

#### Bước 2 — Chấp nhận hoặc Từ chối

- Cửa sổ từ chối: **2 giờ** từ khi nhận task
- Nếu TẤT CẢ team decline → report tự động revert về `Verified`, officer được notify để re-assign

```
PUT /v1/teams/my-tasks/{reportId}/accept   # Assignment → InProgress
PUT /v1/teams/my-tasks/{reportId}/decline  # Assignment → Declined
```

| | |
|---|---|
| **BR** | BR-CLN-007 (2h decline window) |
| **Roles** | CleanupTeam |

---

#### Bước 3 — Cập nhật tiến độ

- Check-in: khoảng cách đến điểm báo cáo ≤ 200m (PostGIS `ST_DWithin` — BR-CLN-002)
- Upload ảnh tiến độ, cập nhật phần trăm (0–100)

```
PUT /v1/reports/{id}/progress
```

| | |
|---|---|
| **BR** | BR-CLN-002 (check-in distance ≤ 200m) |
| **Roles** | CleanupTeam, Admin |

---

#### Bước 4 — Đánh dấu hoàn thành

- Upload **tối thiểu 2 ảnh "after"** có perceptual hash khác nhau (Hamming distance đủ ngưỡng)
- **TẤT CẢ** team được assign phải complete → report mới chuyển `InProgress → Resolved`

```
PUT /v1/reports/{id}/resolve
```

| | |
|---|---|
| **BR** | BR-CLN-004 (2 ảnh after khác hash), BR-CLN-005 (tất cả team phải complete), BR-REP-014 (≥2 after photos) |
| **Roles** | CleanupTeam, Admin |

---

#### Nhánh: Tất cả team Decline

```
[Tất cả Assignment → Declined]
        │
        ▼
Report.Status revert → Verified
        │
        ▼
Officer nhận notification → Re-assign (POST /v1/reports/{id}/assign)
```

---

### 2.5 Inspection Sub-flow *(song song với cleanup)*

> Được tạo sau khi report `Verified`. Chạy **song song**, không block luồng cleanup.

**BR-INS-001 → BR-INS-031**

```
POST   /v1/reports/{id}/inspections          # Tạo inspection case (Officer)
GET    /v1/reports/{id}/inspections          # Danh sách inspections của report
GET    /v1/inspections/queue                 # Hàng đợi inspector
GET    /v1/inspections/{id}                  # Chi tiết inspection
PUT    /v1/inspections/{id}/details          # Ghi nhận bằng chứng vi phạm
PUT    /v1/inspections/{id}/issue-penalty    # Phát hành biên phạt
PUT    /v1/inspections/{id}/close-no-violation  # Đóng: không vi phạm
PUT    /v1/inspections/{id}/record-payment   # Ghi nhận thanh toán phạt
PUT    /v1/inspections/{id}/close            # Đóng inspection
```

| | |
|---|---|
| **Roles** | Officer, Inspector, Admin |

---

## 3. Toàn bộ API Endpoints

### Reports Controller

| Method | Route | Command / Query | Roles | State Transition |
|---|---|---|---|---|
| `POST` | `/v1/reports/analyze` | `AnalyzeReportImageCommand` | Citizen | AI pre-analysis, TTL 15m |
| `POST` | `/v1/reports` | `SubmitPollutionReportCommand` | Citizen | → **Submitted** |
| `GET` | `/v1/reports` | `GetReportsQuery` | All | Paginated list |
| `GET` | `/v1/reports/{id}` | `GetReportByIdQuery` | All | Full detail |
| `GET` | `/v1/reports/my` | `GetMyReportsQuery` | Citizen | Own reports |
| `GET` | `/v1/reports/{id}/history` | `GetReportHistoryQuery` | All | Status timeline |
| `GET` | `/v1/reports/queue` | `GetOfficerQueueQuery` | Officer, DEO, Admin | Priority-sorted queue |
| `GET` | `/v1/reports/company-queue` | `GetCompanyQueueQuery` | CompanyMgr, Admin | Dispatched reports |
| `GET` | `/v1/reports/{id}/progress` | `GetReportProgressQuery` | Officer, Admin | Team status + SLA |
| `GET` | `/v1/reports/progress-board` | `GetReportProgressBoardQuery` | Officer, Admin | Grid + SLA countdown |
| `PUT` | `/v1/reports/{id}/verify` | `VerifyReportCommand` | Officer, Admin | Submitted → **Verified** |
| `PUT` | `/v1/reports/{id}/reject` | `RejectReportCommand` | Officer, Admin | Submitted → **Rejected** |
| `PUT` | `/v1/reports/{id}/waste-tags` | `TagReportWasteCommand` | Officer, Admin | Anytime pre-close |
| `GET` | `/v1/waste-tags` | `GetWasteTagsQuery` | All | Lookup list |
| `POST` | `/v1/reports/{id}/assign` | `AssignTeamCommand` | Officer, Admin | Verified → **InProgress** |
| `POST` | `/v1/reports/{id}/dispatch-to-company` | `DispatchToCompanyCommand` | Officer, Admin | Verified → CompanyId set |
| `POST` | `/v1/reports/{id}/assign-company-team` | `AssignCompanyTeamCommand` | CompanyMgr, Admin | Verified → **InProgress** |
| `PUT` | `/v1/reports/{id}/reassign` | `ReassignTeamCommand` | Officer, Admin | Replace team |
| `PUT` | `/v1/reports/{id}/progress` | `UpdateProgressCommand` | CleanupTeam, Admin | % update + photos |
| `PUT` | `/v1/reports/{id}/resolve` | `ResolveReportCommand` | CleanupTeam, Admin | InProgress → **Resolved** |
| `PUT` | `/v1/reports/{id}/close` | `CloseReportCommand` | All | Resolved → **Closed** |
| `PUT` | `/v1/reports/{id}/reopen` | `ReopenReportCommand` | Citizen | Resolved → **InProgress** |
| `POST` | `/v1/reports/{id}/inspections` | `CreateInspectionReportCommand` | Officer, Admin | Opens inspection |
| `GET` | `/v1/reports/{id}/inspections` | `GetInspectionsByReportQuery` | Officer, Inspector, Admin | Inspection list |

### Teams Controller

| Method | Route | Command / Query | Roles | Assignment Transition |
|---|---|---|---|---|
| `GET` | `/v1/teams/my-profile` | `GetMyTeamProfileQuery` | CleanupTeam | — |
| `GET` | `/v1/teams/my-tasks` | `GetMyAssignmentsQuery` | CleanupTeam | — |
| `GET` | `/v1/teams/my-tasks/{reportId}` | `GetMyTaskDetailQuery` | CleanupTeam | — |
| `PUT` | `/v1/teams/my-tasks/{reportId}/accept` | `AcceptAssignmentCommand` | CleanupTeam | Assigned → **InProgress** |
| `PUT` | `/v1/teams/my-tasks/{reportId}/decline` | `DeclineAssignmentCommand` | CleanupTeam | Assigned → **Declined** |
| `GET` | `/v1/teams/my-progress` | `GetMyProgressHistoryQuery` | CleanupTeam | — |

### Inspections Controller

| Method | Route | Command / Query | Roles | |
|---|---|---|---|---|
| `GET` | `/v1/inspections/queue` | `GetInspectionQueueQuery` | Officer, Admin | — |
| `GET` | `/v1/inspections/{id}` | `GetInspectionReportByIdQuery` | Officer, Inspector, Admin | — |
| `PUT` | `/v1/inspections/{id}/details` | `UpdateInspectionDetailsCommand` | Officer, Admin | Fill evidence |
| `PUT` | `/v1/inspections/{id}/issue-penalty` | `IssuePenaltyCommand` | Officer, Admin | Issue fine |
| `PUT` | `/v1/inspections/{id}/close-no-violation` | `CloseNoViolationCommand` | Officer, Admin | Close clean |
| `PUT` | `/v1/inspections/{id}/record-payment` | `RecordPaymentCommand` | Officer, Admin | Mark paid |
| `PUT` | `/v1/inspections/{id}/close` | `CloseInspectionCommand` | Officer, Admin | Final close |

---

## 4. SLA Reference

> **BR-OFF-020** — Tính từ thời điểm report chuyển sang `Verified`.

| Severity | Resolve SLA | Verify SLA | Auto-close |
|---|---|---|---|
| **Critical** | 3 ngày | — | — |
| **High** | 5 ngày | — | — |
| **Medium** | 7 ngày | — | — |
| **Low** | 10 ngày | — | — |
| *(mọi severity)* | — | 24 giờ | — |
| *(sau Resolved)* | — | — | 7 ngày nếu không hành động |

**Background jobs liên quan:**

- `SlaBreachVerificationJob` — chạy mỗi 15 phút, đánh dấu breach verify SLA
- `SlaBreachResolutionJob` — chạy mỗi 30 phút, đánh dấu breach resolve SLA
- `AutoCloseResolvedReportJob` — chạy hourly, tự đóng report Resolved sau 7 ngày

---

## 5. Background Jobs liên quan

| Job | Lịch | BR | Mô tả |
|---|---|---|---|
| `AutoCloseResolvedReportJob` | Hourly | BR-REP-016, BR-REP-025 | Resolved → Closed sau 7 ngày |
| `SlaBreachVerificationJob` | Every 15m | BR-OFF-002 | Đánh dấu vi phạm SLA verify (24h) |
| `SlaBreachResolutionJob` | Every 30m | BR-OFF-020 | Đánh dấu vi phạm SLA resolve |
| `OverdueReportJob` | Hourly | BR-REP-008, BR-REP-009 | Notify báo cáo quá hạn |
| `AiRetryJob` | Every 5m | BR-AI-006 | Retry phân tích AI thất bại |
| `DraftCleanupJob` | Daily | BR-REP-019 | Xóa draft cũ |

---

*Đồng bộ với: `SU26SE049_BusinessRules_v1_0.docx` v1.0 — Last updated: 2026-06-22*

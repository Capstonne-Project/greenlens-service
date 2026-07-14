# Session Handoff — GreenLens Backend

> **Cập nhật lần cuối:** 2026-07-13 22:21 · **Phiên bản:** 18 · **Agent:** Composer

## 0. TL;DR

Backend .NET 9 GreenLens. Phiên 18 **hoàn thành BR-REP-030..033 duplicate detection** trên branch `feature/duplicate-ai-compare-image` (2 commits, working tree clean). Tier 1 geo inline + Tier 2 AI compare background job, LEO review flow (confirm/dismiss/flag/candidates), BR-REP-032 merge media + 50% points. Đã tách 2 migration, fix Haversine NaN và race condition Tier 2 job. **Việc tiếp theo:** apply migration lên DB dev → push/PR → deploy Python `/api/v1/compare-images`.

## 1. Mục tiêu & Bối cảnh

- **Mục tiêu tổng thể:** Backend .NET 9 cho ứng dụng báo cáo ô nhiễm môi trường (SU26SE049)
- **Phạm vi phiên 18:** BR-REP-030..033 — duplicate detection (geo Tier 1 + AI Tier 2 + officer review + merge)
- **Branch hiện tại:** `feature/duplicate-ai-compare-image` (chưa merge develop)
- **Ngôn ngữ:** Tiếng Việt (giao tiếp + XML doc BR), English (code)

## 2. Quyết định đã chốt (Locked Decisions)

| #   | Quyết định                                                                                                        | Lý do                                                                                                       | Ngày       |
| --- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ---------- |
| 1–41 | *(giữ nguyên từ phiên 17 — xem Change Log)*                                                                      | Cleanup/Inspection/Admin/Company conventions                                                                | ≤2026-07-11 |
| 42  | **BR-REP-030 Tier 1 inline** trong SubmitHandler (Haversine + bbox, không PostGIS point column)                   | Citizen cần biết ngay; Report dùng decimal lat/lng                                                          | 2026-07-10 |
| 43  | **BR-REP-030 Tier 2 background** qua Hangfire `CompareDuplicateImagesJob`                                         | Tránh block submit p95; AI timeout 5s fallback giữ Tier 1                                                   | 2026-07-10 |
| 44  | **Tier 2 gọi Python** `/api/v1/compare-images` (DINOv2), không load ML vào .NET                                 | Cùng pattern AiImageCompareService + HttpClient "AiService"                                                   | 2026-07-10 |
| 45  | **BR-REP-032 điểm +50%** = `round(ReportVerified × 0.5)` runtime; `DuplicateReport` config chỉ là kill-switch      | BR v1.2: điểm động theo base ReportVerified, không dùng Points cố định seed                                   | 2026-07-13 |
| 46  | **BR-REP-032 merge ảnh** qua `ReportMedia.ReassignToReport()` trước `MarkDuplicate`                               | Comments merge **deferred** — chưa có Comment entity                                                        | 2026-07-13 |
| 47  | **Migration tách 2 file** (chưa apply DB lúc chốt phiên)                                                          | Tránh bundle unrelated schema drift penalty_payments soft-delete                                              | 2026-07-13 |
| 48  | **Tier 2 job reload sau AI call** + domain guard `Status Duplicate/Rejected`                                        | Fix lost-update race khi LEO confirm duplicate concurrent với background job                                  | 2026-07-13 |
| 49  | **Haversine → `GeoMath.HaversineMeters`** với `Math.Clamp(a,0,1)` + `Asin(√a)`                                   | Tránh NaN khi FP rounding đẩy `a > 1`                                                                       | 2026-07-13 |
| 50  | **Branch/commit naming: KHÔNG dùng mã BR** trong tên                                                              | User preference từ phiên trước                                                                                | 2026-06-28 |

## 3. Trạng thái hiện tại

### ✅ Đã hoàn thành (phiên 18 — 2026-07-13)

**Duplicate Detection (BR-REP-030..033):**

- **Domain** (`Report.cs`, `ReportEvents.cs`, `ReportMedia.cs`):
  - Fields: `IsPossibleDuplicate`, `PossibleDuplicateOfReportId`, `DuplicateDetectionSource`, `AiSimilarityScore`
  - Methods: `MarkPossibleDuplicate`, `DismissDuplicate`, `ApplyDuplicateAiResult` (guard Duplicate/Rejected), `MarkDuplicate`, `ReportMedia.ReassignToReport`
  - Events: `ReportPossibleDuplicateFlaggedEvent`, `ReportMarkedDuplicateEvent`
- **Application**:
  - Tier 1 inline trong `SubmitPollutionReportCommandHandler` (bbox + `GeoMath.HaversineMeters` ≤ 50m, same category, 24h)
  - Slices: `ConfirmDuplicate`, `DismissDuplicate`, `FlagReport`, `GetDuplicateCandidates`
  - Event handlers: `EnqueueDuplicateCompareHandler`, `DuplicateMergedPointsHandler` (+50%), `DuplicateMergedNotificationHandler`
  - Interfaces: `IAiImageCompareService`, `IDuplicateCompareScheduler`
- **Infrastructure**:
  - `AiImageCompareService` → POST `/api/v1/compare-images`
  - `CompareDuplicateImagesJob` (idempotent, reload-after-AI)
  - `DuplicateCompareScheduler`
- **API** (`ReportsController`): `GET duplicate-candidates`, `POST {id}/confirm-duplicate`, `POST {id}/dismiss-duplicate`, `POST {id}/flag`
- **Hardening** (commit `6958293`):
  - `GeoMath.cs` + `GeoMathTests.cs`
  - Race fix `CompareDuplicateImagesJob` + domain test `ApplyDuplicateAiResult_WhenAlreadyDuplicate_IsNoOp`

**Migrations (tách, chưa apply DB):**

| Migration | Nội dung |
|-----------|----------|
| `20260713145829_202607132010_AddPenaltyPaymentSoftDelete` | `penalty_payments.deleted_at/deleted_by` only |
| `20260713145907_202607132015_AddDuplicateDetectionFields` | reports duplicate columns + indexes + FK + gamification seed description |

**Commits trên branch:**

```
6958293 fix(reports): prevent Haversine NaN and stale Tier 2 duplicate overwrites
f2a1cfc feat(reports): add geo and AI image duplicate detection with officer review flow
```

**Tests:** Domain ~85 pass, Application ~80 pass (last run phiên 18).

### ✅ Đã hoàn thành (tóm tắt phiên 17 trở về trước)

- Cleanup 8/8, Inspection 14/14, Admin 12/12, Company 14/14, Gamification, Notifications, BR-AUTH/ORG batches — xem Change Log.

### ⏳ Chưa làm / deferred

- Comments merge (BR-REP-032) — chờ Comment entity
- `br_v12_comparison_report.md` chưa cập nhật duplicate detection
- API Documentation v2.1+ chưa có duplicate endpoints
- Migration **chưa** `database update` (user xác nhận chưa apply)

## 4. Việc tiếp theo (Next Steps)

- [ ] **Apply migrations** trên DB dev:
  ```powershell
  dotnet ef database update --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api
  ```
- [ ] **Push branch + tạo PR** `feature/duplicate-ai-compare-image` → develop
- [ ] **Python AI service**: deploy endpoint `/api/v1/compare-images` (spec: `docs/ImageCompareAi/ai-compare-images-spec.md`)
- [ ] Cấu hình `AiService` base URL + timeout trong user-secrets / env (dev)
- [ ] Cập nhật `docs/BusinessRule/br_v12_comparison_report.md` — Reports duplicate 4/4 rules
- [ ] Cập nhật API Documentation v2.1 (duplicate endpoints cho FE/LEO)
- [ ] Integration test: submit 2 reports cùng vị trí → Tier 1 flag → Tier 2 upgrade/dismiss
- [ ] **Comments module** (BR-CMT-001..004) — sau khi có entity
- [ ] Map module (BR-MAP-*), Rate limiting, AiRetryJob — backlog từ phiên 17

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|-----------|---------|------------|
| `docs/ImageCompareAi/ai-compare-images-spec.md` | Spec Python AI compare | ✅ Có sẵn |
| `docs/ImageCompareAi/implementation_plan_compare_ai.md` | Plan implement .NET | ✅ Implemented |
| `docs/ImageCompareAi/dotnet-compare-images-client.md` | Hướng dẫn HTTP client | ✅ Mới |
| `docs/BusinessRule/SU26SE049_BusinessRules_v1_2.md` | BR source (REP-032 +50%) | ✅ Reference |
| `src/Greenlens.Application/Common/GeoMath.cs` | Haversine safe distance | ✅ Mới (phiên 18) |
| `src/Greenlens.Infrastructure/BackgroundJobs/CompareDuplicateImagesJob.cs` | Tier 2 AI job | ✅ Hardened |
| `src/Greenlens.Application/Features/Reports/ConfirmDuplicate/` | LEO confirm merge | ✅ Mới |
| `src/Greenlens.Application/Features/Reports/DuplicateDetection/EventHandlers/` | Points + notify + enqueue | ✅ Mới |
| `src/Greenlens.Infrastructure/Ai/AiImageCompareService.cs` | HTTP adapter Tier 2 | ✅ Mới |
| `src/Greenlens.Infrastructure/Persistence/Migrations/20260713145829_*` | Penalty soft-delete | ✅ Chưa apply |
| `src/Greenlens.Infrastructure/Persistence/Migrations/20260713145907_*` | Duplicate fields | ✅ Chưa apply |
| `tests/Greenlens.Application.UnitTests/GeoMathTests.cs` | Haversine regression | ✅ Mới |
| `tests/Greenlens.Domain.UnitTests/ReportTests.cs` | Duplicate domain tests | ✅ Extended |

## 6. Kiến thức nền & Quy ước

- **Tech stack:** .NET 9, ASP.NET Core, EF Core 9, PostgreSQL + PostGIS, Hangfire, MediatR, FluentValidation, Mapster
- **Architecture:** Clean Architecture — Domain → Application → Infrastructure ← Api
- **Duplicate detection flow:**
  ```
  Submit → Tier 1 (geo+time+category) → MarkPossibleDuplicate("geo_time")
         → DomainEvent → Enqueue CompareDuplicateImagesJob
         → Tier 2 AI (5s timeout) → upgrade "geo_time_ai" OR dismiss
         → LEO: confirm-duplicate (merge) / dismiss-duplicate / flag
  ```
- **Report.Location:** `decimal Latitude/Longitude` — Tier 1 dùng bbox + `GeoMath.HaversineMeters`, KHÔNG PostGIS point column
- **PostGIS** chỉ dùng cho check-in distance (`IGeoDistanceService`), không cho duplicate Tier 1
- **Tier 2 idempotency:** chỉ apply khi `IsPossibleDuplicate && DuplicateDetectionSource == "geo_time" && Status ∉ {Duplicate, Rejected}`
- **Migration apply:** `dotnet ef database update --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api`
- **Build/Test:** `dotnet build -v q` · `dotnet test --no-build`
- **Git:** Conventional Commits; branch `feature/<slug>`; KHÔNG mã BR trong tên commit/branch

## 7. Câu hỏi mở / Cần xác nhận

- Python AI service infra deploy (Railway/Render/AWS) — DINOv2-base ~1.5GB RAM min
- User chưa apply migration — cần chạy `database update` trước khi test E2E
- Comments merge BR-REP-032 — chờ Comment entity; có implement sau không?
- PR merge vào `develop` — user chưa yêu cầu push

## 8. Thuật ngữ

| Thuật ngữ | Nghĩa |
|-----------|-------|
| Tier 1 | Duplicate detection geo ≤50m + same category + ≤24h (inline submit) |
| Tier 2 | AI image compare DINOv2 via `/api/v1/compare-images` (background) |
| `geo_time` | Source flag sau Tier 1, chờ Tier 2 |
| `geo_time_ai` | Tier 1 + AI xác nhận same scene |
| LEO confirm | `ConfirmDuplicate` → merge media, MarkDuplicate, +50% points, notify |

## 9. Change Log

- 2026-07-11 — Session v17: Cleanup 8/8 + Inspection 14/14 + mobile API docs. ~79%
- 2026-07-13 — **Session v18:** BR-REP-030..033 duplicate detection hoàn thành. Tier 1+2, officer review, BR-REP-032 media merge + 50% points. Tách 2 migrations. Fix Haversine NaN (`GeoMath`) + Tier 2 race condition. Branch `feature/duplicate-ai-compare-image`, 2 commits. ~83% (ước tính).

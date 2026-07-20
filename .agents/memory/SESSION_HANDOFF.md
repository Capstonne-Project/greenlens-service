# Session Handoff — GreenLens Backend

> **Cập nhật lần cuối:** 2026-07-15 15:30 · **Phiên bản:** 20 · **Agent:** Antigravity

## 0. TL;DR

Backend .NET 9 GreenLens trên branch **`develop`** — Phiên 20: Đã cập nhật thành công luồng **Citizen satisfaction** vào `GET /reports/{id}` (`ReportSatisfactionInfo` & `HasCurrentUserRated`) cho Citizen có thể xem rating của mình. Các migrations DB dev (Comments, BlockedWords, Duplicate, Penalty) **đã được apply hoàn tất**. Ngoài ra, đã cập nhật sơ đồ **Conceptual ERD** (`docs/BusinessRule/conceptual-erd.md`) theo chuẩn mới (6 nhóm actor, tách Department/LocalOffice, và bổ sung đủ 43 entities). **Việc tiếp theo:** Triển khai các backlog còn lại như Unique index cho `report_satisfactions`, API Rate limit, hoặc chuyển sang setup Python AI service.

## 1. Mục tiêu & Bối cảnh

- **Mục tiêu tổng thể:** Backend .NET 9 crowdsourcing báo cáo ô nhiễm (SU26SE049)
- **Phạm vi phiên 20:** Implement satisfaction vào API GetReportById, verify DB migrations, cập nhật docs kiến trúc.
- **Branch hiện tại:** `develop` (feature branches đã merge)
- **Ngôn ngữ:** Tiếng Việt (giao tiếp + XML doc BR), English (code)

## 2. Quyết định đã chốt (Locked Decisions)

| #   | Quyết định                                                                                                        | Lý do                                                                                                       | Ngày       |
| --- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ---------- |
| 1–53 | *(giữ nguyên phiên 18 — duplicate, geo Tier 1/2, naming không BR trong commit)*                                  | Xem Change Log                                                                                              | ≤2026-07-14 |
| 54  | **Citizen satisfaction: 2 luồng tách** — `close`/`reopen` (status) vs `POST /rate` (analytics, không đổi status) | BR-REP-015 vs BR-REP-018; `isSatisfied:false` không auto reopen                                             | 2026-07-15 |
| 55  | **Docs FE changelog** gom trong `docs/Changelogs/`                                                                | User preference di chuyển guide từ `docs/` root                                                             | 2026-07-15 |
| 56  | **Branch/commit naming: KHÔNG dùng mã BR** trong tên                                                              | User preference                                                                                               | 2026-06-28 |
| 57  | **Conceptual ERD cập nhật mới nhất:** Phân tách rõ 6 actor và bổ sung full 43 entities (kể cả các join/audit/log)| Để align với DB (ApplicationDbContext) và Logical ERD thực tế                                               | 2026-07-15 |

## 3. Trạng thái hiện tại

### ✅ Hoàn thành trong Phiên 20 (2026-07-15)

- **Database Migrations:** Đã apply thành công toàn bộ các migration tồn đọng (Comments, BlockedWords, Duplicate, Penalty soft-delete) lên môi trường dev.
- **Satisfaction API Integration:** Đã cập nhật `GetReportByIdQueryHandler` & `ReportDetailResponse` trả về `ReportSatisfactionInfo?` và cờ `HasCurrentUserRated`. (Build và test 206/206 passed).
- **System Documentation:** Cập nhật `docs/BusinessRule/conceptual-erd.md`, thêm annotations cho Actor (Citizen, LEO, DEO, Cleanup Team, Inspection Team, Admin) và 10 entities bị thiếu.

### ⏳ Chưa commit / chưa làm

| Item | Trạng thái |
|------|------------|
| `docs/BusinessRule/br_v12_comparison_report.md` | Modified, chưa commit (format + cập nhật coverage ~89%) |
| Cập nhật codebase với `br_v12_comparison_report.md` | Có thể cần commit lại nếu user đồng ý |
| Unique index `(report_id, user_id)` trên `report_satisfactions` | Chưa có |
| Profanity filter trên `comment` trong `POST /rate` | Chưa có |
| Unit test handler `RateReport` | Chỉ có entity test |
| Global API rate limit middleware (BR-SYS-004) | Backlog |
| Map module còn lại (BR-MAP-*) | Backlog |

## 4. Việc tiếp theo (Next Steps)

- [ ] Commit các thay đổi BE và documentation của phiên này (`GetReportById` update, ERD update, BR doc).
- [ ] E2E smoke: comments, submit rate limit, blocked words admin, duplicate flow, `POST /rate` sau Resolved
- [ ] Thêm Unique index `(report_id, user_id)` cho entity `ReportSatisfaction` (cần tạo migration mới)
- [ ] Áp dụng Profanity filter vào command/handler của việc rate report.
- [ ] Deploy / cấu hình Python `/api/v1/compare-images` + `AiService` URL (Tier 2 duplicate)
- [ ] Cấu hình Redis dev (`ConnectionStrings:Redis`) cho BR-REP-010 production-like test
- [ ] Integration test: submit duplicate geo → Tier 2 job
- [ ] Map module, BR-SYS-004 global rate limit

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|-----------|---------|------------|
| `docs/BusinessRule/conceptual-erd.md` | Tài liệu Conceptual ERD 43 entities | ✅ Đã cập nhật (phiên 20) |
| `src/Greenlens.Application/Features/Reports/GetReportById/` | Handler & DTO trả về report detail (có satisfaction) | ✅ Đã update (phiên 20) |
| `docs/Changelogs/fe-citizen-satisfaction-api-guide.md` | FE: rate/close/reopen sau Resolved | ✅ Có sẵn |
| `docs/BusinessRule/br_v12_comparison_report.md` | BR coverage matrix | ⚠️ Modified, chưa commit |
| `src/.../Comments/` | BR-CMT-001..004 | ✅ Có sẵn |

## 6. Kiến thức nền & Quy ước

- **Tech stack:** .NET 9, EF Core 9, PostgreSQL + PostGIS, Redis (optional dev), Hangfire, MediatR
- **Citizen post-resolution flow (Phase 5):**
  ```
  Resolved → Citizen: PUT /close (hài lòng) | PUT /reopen (max 2, 7 ngày) | POST /rate (1 lần)
           → Auto-close 7 ngày (job) → Closed → vẫn POST /rate được
  ```
- **Profanity:** `IProfanityFilter` đọc `IBlockedWordCache` — áp submit description + comments; **chưa** áp rate comment
- **Submit rate limit:** 5/h + 20/24h per user; HTTP 429 `RATE_LIMIT_EXCEEDED`
- **Duplicate flow:** Tier 1 inline, Tier 2 Hangfire + Python
- **Build/Test:** `dotnet build -v q` · `dotnet test --no-build`
- **Git:** Conventional Commits; **không** mã BR trong tên branch/commit

## 7. Câu hỏi mở / Cần xác nhận

- Có muốn làm tiếp phần **Unique Index cho Satisfaction** và **Profanity filter cho rate comment** không? Hay chuyển sang commit code và làm Map module / AI Python service?

## 8. Thuật ngữ

| Thuật ngữ | Nghĩa |
|-----------|-------|
| Satisfaction / rate | Đánh giá chất lượng — `report_satisfactions`, không đổi status |
| Close / reopen | Xác nhận kết quả xử lý — BR-REP-015, đổi status |
| Blocked words | Từ cấm profanity — Admin CRUD, cache in-memory |
| Tier 1 / Tier 2 | Duplicate geo inline / AI background |

## 9. Change Log

- 2026-07-13 — Session v18: BR-REP-030..033 duplicate detection. ~83%
- 2026-07-14/15 — PR #45: Comments, AiRetryJob, blocked words admin, submit guards (REP-004/010/011), docs changelogs. Merge `develop`.
- 2026-07-15 — Session v19: Audit satisfaction flow (đã có API); FE guide `fe-citizen-satisfaction-api-guide.md`; sửa docs lỗi thời.
- 2026-07-15 — **Session v20:** Apply migrations dev DB. Cập nhật `GetReportById` trả về thông tin Satisfaction (`HasCurrentUserRated`). Hoàn thiện toàn bộ `conceptual-erd.md` (6 actors, 43 entities).

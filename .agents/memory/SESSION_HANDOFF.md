# Session Handoff — GreenLens Backend

> **Cập nhật lần cuối:** 2026-07-26 10:49 · **Phiên bản:** 21 · **Agent:** Antigravity

## 0. TL;DR

Backend .NET 9 GreenLens trên branch **`develop`**. Phiên 21 (kéo dài 22-07 → 26-07): Hoàn thành bộ **8 Class Diagrams + 1 Architecture Layer** trong `CLASS_DIAGRAMS.md`, **22 Sequence Diagrams** trong `SEQUENCE_DIAGRAMS.md` (đã bổ sung bad case cho 8 diagram còn thiếu). Thử nghiệm chuyển đổi sang **PlantUML** để import draw.io (1 file test SD-62). Đã xác nhận document FE LEO duplicate detection đầy đủ. **Việc tiếp theo:** Hoàn thành chuyển 22 diagram sang PlantUML nếu draw.io test OK; commit docs; tiếp tục backlog code (Unique index satisfaction, Profanity filter rate, Map module).

## 1. Mục tiêu & Bối cảnh

- **Mục tiêu tổng thể:** Backend .NET 9 crowdsourcing báo cáo ô nhiễm (SU26SE049)
- **Phạm vi phiên 21:** Vẽ UML diagrams cho tài liệu bảo vệ (Class + Sequence), bổ sung bad case, chuyển đổi PlantUML
- **Branch hiện tại:** `develop`
- **Ngôn ngữ:** Tiếng Việt (giao tiếp + XML doc BR), English (code)

## 2. Quyết định đã chốt (Locked Decisions)

| #   | Quyết định                                                                                                        | Lý do                                                                                                       | Ngày       |
| --- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ---------- |
| 1–57 | *(giữ nguyên phiên 20 — xem Change Log)*                                                                        | Xem Change Log                                                                                              | ≤2026-07-15 |
| 58  | **Sequence Diagram chuẩn:** Có UI object (Mobile App/Web App) ở đầu, Database ở cuối; Object BE có dấu `:` (vd `:AuthController`) | Tuân thủ UML chuẩn cho bảo vệ đồ án                                                                        | 2026-07-21 |
| 59  | **Phân loại Actor theo thiết bị:** Citizen, Cleaner, Inspector, CompanyStaff → Mobile App; DEO, LEO, Admin, CompanyManager → Web App | Mapping thực tế hệ thống                                                                                   | 2026-07-22 |
| 60  | **Class Diagram format:** `+ FieldName : Type` (có khoảng trống sau `+` và trước `:`)                             | Tuân thủ UML notation chuẩn                                                                                 | 2026-07-21 |
| 61  | **Chuyển PlantUML thay Mermaid cho draw.io:** Mermaid import draw.io bị lỗi (object dưới đáy, lifeline nét liền, màu tím). PlantUML mặc định đúng chuẩn | draw.io Mermaid parser không hỗ trợ `mirrorActors:false` và theme directive                                  | 2026-07-22 |
| 62  | **22/22 Sequence Diagram đều có bad case** (alt blocks cho error paths)                                            | Đầy đủ cho bảo vệ khi thầy/cô hỏi "nếu xảy ra lỗi thì sao?"                                               | 2026-07-22 |

## 3. Trạng thái hiện tại

### ✅ Hoàn thành trong Phiên 21

**Documentation / Diagrams:**
- **8 Class Diagrams + Architecture Layer (CD-09)** trong `docs/BusinessRule/CLASS_DIAGRAMS.md`
  - CD-01 User & Auth, CD-02 Report Core, CD-03 Review & Assignment, CD-04 Cleanup, CD-05 Gamification, CD-06 Comments & Notifications, CD-07 Administration, CD-08 Company & Penalty, CD-09 Architecture Layer
- **22 Sequence Diagrams** trong `docs/BusinessRule/SEQUENCE_DIAGRAMS.md` — tất cả có happy + bad case
  - 14 diagram đã có bad case từ đầu, **bổ sung bad case cho 8 diagram:** SD-01 (email trùng 409), SD-13 (team inactive/khác office 400), SD-15 (ảnh After < 2, BR-CLN-004), SD-28 (đã có inspection 409), SD-29 (team type sai 400), SD-36 (dept/office trùng province/ward 409), SD-37 (user role mismatch 400), SD-38 (TaxCode trùng 409, dates invalid 400)
- **24 file `.mmd`** trích xuất trong `docs/BusinessRule/mermaid-exports/` (đã thêm `%%{init}%%` directive — nhưng draw.io không hỗ trợ)
- **1 file PlantUML test** `docs/BusinessRule/plantuml-exports/SD-62_View_Audit_Logs.puml` — chờ user test trên draw.io
- **Script:** `docs/BusinessRule/extract-mermaid.ps1` (trích xuất mermaid từ md), `update-mmd-with-theme.ps1` (cập nhật directive)

**Q&A / Knowledge sharing:**
- Giải thích chi tiết **EXIF metadata validation** (BR-REP-011): flow, 3 layers, 2 reason codes, 2 outputs
- Giải thích **Duplicate detection** 2 tầng: Tier 1 inline (Haversine ≤50m + category + 24h), Tier 2 background (Hangfire → Python AI DINOv2)
- Xác nhận document FE LEO duplicate detection (`docs/Changelogs/fe-leo-duplicate-detection-guide.md`) đã đầy đủ 325 dòng, 10 sections

### ⏳ Chưa hoàn thành

| Item | Trạng thái |
|------|------------|
| Chuyển 22 diagram sang PlantUML | Chỉ làm 1 file test (SD-62), chờ user xác nhận draw.io OK |
| `docs/BusinessRule/br_v12_comparison_report.md` | Modified, chưa commit |
| Unique index `(report_id, user_id)` trên `report_satisfactions` | Chưa có |
| Profanity filter trên `POST /rate` | Chưa có |
| Unit test handler `RateReport` | Chỉ có entity test |
| Global API rate limit middleware (BR-SYS-004) | Backlog |
| Map module còn lại (BR-MAP-*) | Backlog |

## 4. Việc tiếp theo (Next Steps)

- [ ] User test PlantUML file SD-62 trên draw.io → nếu OK, convert hết 22 diagram
- [ ] Commit docs: CLASS_DIAGRAMS.md, SEQUENCE_DIAGRAMS.md, br_v12_comparison_report.md
- [ ] Unique index + migration cho `report_satisfactions`
- [ ] Profanity filter cho rate comment
- [ ] E2E smoke: duplicate flow, comments, blocked words, rate
- [ ] Deploy / cấu hình Python AI service
- [ ] Map module, BR-SYS-004 global rate limit

## 5. File & Artefact quan trọng

| Đường dẫn | Vai trò | Trạng thái |
|-----------|---------|------------|
| `docs/BusinessRule/CLASS_DIAGRAMS.md` | 8 Class Diagrams + Architecture Layer | ✅ Hoàn thành (phiên 21) |
| `docs/BusinessRule/SEQUENCE_DIAGRAMS.md` | 22 Sequence Diagrams (happy + bad case) | ✅ Hoàn thành (phiên 21) |
| `docs/BusinessRule/plantuml-exports/SD-62_View_Audit_Logs.puml` | PlantUML test file | ⏳ Chờ user test draw.io |
| `docs/BusinessRule/mermaid-exports/*.mmd` | 24 file Mermaid trích xuất | ⚠️ draw.io không hỗ trợ directive |
| `docs/BusinessRule/br_v12_comparison_report.md` | BR coverage matrix | ⚠️ Modified, chưa commit |
| `docs/Changelogs/fe-leo-duplicate-detection-guide.md` | FE: LEO duplicate detection guide | ✅ Đầy đủ 325 dòng |
| `docs/BusinessRule/conceptual-erd.md` | Conceptual ERD 43 entities | ✅ Đã cập nhật (phiên 20) |

## 6. Kiến thức nền & Quy ước

- **Tech stack:** .NET 9, EF Core 9, PostgreSQL + PostGIS, Redis (optional dev), Hangfire, MediatR
- **Diagram conventions:**
  - Sequence: UI object đầu (Mobile/Web App), Database cuối, BE object có `:` prefix
  - Actor → device mapping: Citizen/Cleaner/Inspector/CompanyStaff → Mobile; DEO/LEO/Admin/CompanyManager → Web
  - Class: `+ FieldName : Type` format, PascalCase
  - PlantUML preferred over Mermaid cho draw.io export (lifeline dashed, no bottom boxes, black theme)
- **EXIF validation (BR-REP-011):** `IImageExifAnalyzer` (Application) → `MetadataExtractorImageExifAnalyzer` (Infrastructure) → `ExifSuspicionEvaluator` (Application pure rules) → `Report.FlagSuspicious()` (Domain). 2 codes: `EXIF_METADATA_MISSING`, `EXIF_TIMESTAMP_STALE` (>1h)
- **Duplicate detection (BR-REP-030):** Tier 1 inline (Haversine) → `MarkPossibleDuplicate` → DomainEvent → `EnqueueDuplicateCompareHandler` → Hangfire `CompareDuplicateImagesJob` → Python AI → `ApplyDuplicateAiResult`
- **Build/Test:** `dotnet build -v q` · `dotnet test --no-build`
- **Git:** Conventional Commits; **không** mã BR trong tên branch/commit

## 7. Câu hỏi mở / Cần xác nhận

- User đã test PlantUML trên draw.io chưa? Nếu OK → convert hết 22 diagram
- Có muốn commit docs trước hay tiếp tục chỉnh sửa thêm?
- Priority tiếp: docs còn lại hay code features (satisfaction unique index, profanity, map)?

## 8. Thuật ngữ

| Thuật ngữ | Nghĩa |
|-----------|-------|
| Satisfaction / rate | Đánh giá chất lượng — `report_satisfactions`, không đổi status |
| Close / reopen | Xác nhận kết quả xử lý — BR-REP-015, đổi status |
| Tier 1 / Tier 2 | Duplicate geo inline / AI background |
| PlantUML | Ngôn ngữ UML text-based, draw.io hỗ trợ import tốt hơn Mermaid |
| LEO | Local Environmental Officer — cán bộ môi trường cấp phường |
| DEO | Department Environmental Officer — cán bộ cấp sở |

## 9. Change Log

- 2026-07-13 — Session v18: BR-REP-030..033 duplicate detection. ~83%
- 2026-07-14/15 — PR #45: Comments, AiRetryJob, blocked words admin, submit guards (REP-004/010/011), docs changelogs. Merge `develop`.
- 2026-07-15 — Session v19: Audit satisfaction flow (đã có API); FE guide `fe-citizen-satisfaction-api-guide.md`.
- 2026-07-15 — Session v20: Apply migrations dev DB. Cập nhật `GetReportById` trả satisfaction. Hoàn thiện ERD 43 entities.
- 2026-07-22~26 — **Session v21:** Vẽ 8 Class Diagrams + Architecture Layer + 22 Sequence Diagrams. Bổ sung bad case cho 8 SD còn thiếu. Thử PlantUML cho draw.io (1 test file). Giải thích EXIF validation + Duplicate detection architecture. Xác nhận FE LEO duplicate guide đầy đủ.

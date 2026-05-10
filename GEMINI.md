# EcoReport Backend Skills (Workspace-specific)

Bộ skill workspace-specific cho dự án **SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution**, viết theo chuẩn Antigravity Skills.

Đi kèm `CLAUDE.md` và `BusinessRules` của dự án. Bộ skill này hướng dẫn Claude scaffold mã backend .NET 9 Clean Architecture đúng convention mà không cần lặp lại hướng dẫn trong từng prompt.

## Có gì trong này

| Skill | Trigger khi user yêu cầu… |
|---|---|
| **`ecoreport-feature-slice`** | thêm command/query/use case/feature/handler/validator vào `Application/Features/` |
| **`ecoreport-repository-pattern`** | thêm repository — và **đầu tiên** kiểm tra xem có thực sự cần repo hay không |
| **`ecoreport-controller-base`** | thêm endpoint/controller/route/action HTTP (kiểu `ControllerBase`) |
| **`ecoreport-efcore-best-practices`** | bất cứ thay đổi nào liên quan EF Core — entity, migration, query, indexing, geo (PostGIS), perf, audit, soft delete |

Mỗi skill là một thư mục độc lập với `SKILL.md` + `assets/` (template `.cs.template`) + (tuỳ skill) `references/` (doc chi tiết, load on-demand để giữ context nhỏ — đúng nguyên tắc *progressive disclosure*).

## Cấu trúc

```
skills/
├── ecoreport-feature-slice/
│   ├── SKILL.md
│   └── assets/
│       ├── command-slice.cs.template
│       └── query-slice.cs.template
│
├── ecoreport-repository-pattern/
│   ├── SKILL.md
│   └── assets/
│       ├── repository.cs.template
│       └── specification.cs.template
│
├── ecoreport-controller-base/
│   ├── SKILL.md
│   └── assets/
│       ├── controller.cs.template
│       ├── action-snippet.cs.template
│       └── result-extensions.cs.template
│
└── ecoreport-efcore-best-practices/
    ├── SKILL.md
    ├── references/
    │   ├── configuration.md
    │   ├── queries-and-projection.md
    │   ├── migrations.md
    │   ├── geo-postgis.md
    │   ├── performance.md
    │   └── auditing-and-soft-delete.md
    └── assets/
        ├── entity-configuration.cs.template
        ├── db-context.cs.template
        ├── auditing-interceptor.cs.template
        └── soft-delete-interceptor.cs.template
```

## Cài đặt vào Antigravity workspace

1. Trong workspace của dự án (cùng repo với backend), tạo thư mục `.agents/skills/` (hoặc theo path mà cài đặt Antigravity của bạn dùng — kiểm tra phần "Workspace skills" trong settings).
2. Copy 4 thư mục skill vào đó:

   ```bash
   cp -r skills/ecoreport-* .agents/skills/
   ```

3. Reload workspace. Claude sẽ tự discover skill qua `SKILL.md` frontmatter và sử dụng đúng lúc.

> **Lưu ý:** đường dẫn cài đặt skill có thể khác giữa các phiên bản Antigravity. Nếu Antigravity workspace của bạn dùng path khác (ví dụ `.agents/skills/`), copy vào đó. Logic skill (frontmatter + body) là chuẩn chung, không phụ thuộc path.

## Triết lý khi viết bộ skill này

1. **Skill nhỏ, trigger rõ.** Một skill cho một concern. Description nêu rõ các phrasing user thường dùng (kể cả casual: "fix the N+1", "add an endpoint") — chống *under-triggering*.
2. **Progressive disclosure.** SKILL.md ngắn (< 500 dòng); chi tiết nặng được tách sang `references/` và chỉ load on-demand. Skill EF Core áp dụng pattern này.
3. **Templates với placeholder rõ ràng.** Mọi placeholder dùng `__DOUBLE_UNDERSCORE__` để find-replace toàn cục an toàn. Khi Claude đọc template, chỉ cần map placeholder → giá trị thật của use case hiện tại.
4. **Skill là *gatekeeper*, không chỉ là *generator*.** Skill `repository-pattern` không tạo ngay — nó **gate** trước, từ chối khi user yêu cầu nhưng không có lý do thật. Đó là thứ giúp codebase không bị over-engineer.
5. **Mọi BR ID đều bắt nguồn từ tài liệu chuẩn.** Skill yêu cầu Claude *hỏi* khi không chắc BR ID — không bịa. Mỗi handler scaffold ra đều có XML comment `Implements: BR-...` để team grep được coverage.
6. **Convention là một sản phẩm, không phải sở thích.** Skill enforce convention từ `OVERVIEW.md`: dependency rule, vertical slice, Result pattern, AsNoTracking + projection, audit interceptor, soft delete query filter, GIST index cho geo. Mỗi quy tắc đều có lý do — skill nêu lý do, không chỉ ra lệnh.

## Khi nào KHÔNG dùng các skill này

- Dự án khác (không phải SU26SE049). Bộ skill này hard-code tên project, namespace, BR module prefix. Fork và tổng quát hoá nếu bạn muốn dùng cho dự án khác.
- Frontend, mobile, hoặc Infrastructure-as-Code. Bộ này chỉ cover backend .NET.
- Quyết định kiến trúc lớn (đổi từ PostgreSQL sang SQL Server, đổi MediatR sang một mediator khác). Skill chỉ thực thi convention đã chốt; thay đổi convention là thảo luận với team trước, sau đó cập nhật `OVERVIEW.md` + skill cùng nhau.

## Mở rộng

Cần skill mới? Pattern đã ổn:

- Một `SKILL.md` với YAML frontmatter (`name`, `description` "đẩy" — nói rõ các phrasing trigger cả casual lẫn kỹ thuật).
- `assets/*.cs.template` với placeholder `__SCREAMING__`.
- Nếu skill phức tạp, thêm `references/<topic>.md` và trỏ trong SKILL.md theo bảng "load reference khi…".

Skill ý tưởng cho lần mở rộng tiếp theo:
- `greenlens-domain-entity` — scaffold entity với state machine + domain events
- `greenlens-mediatr-behavior` — pipeline behaviors (validation, transaction, caching, logging)
- `greenlens-background-job` — Hangfire job với DI + retry + idempotency
- `greenlens-integration-test` — Testcontainers Postgres + WebApplicationFactory boilerplate
- `greenlens-api-error-mapping` — mở rộng `ToActionResult` cho error type mới

---

**Phiên bản:** 1.1
**Đồng bộ với:** `OVERVIEW.md` v1.2 + `SU26SE049_BusinessRules_v1_0.docx` v1.0 (17/04/2026).
**Changelog:**
- v1.1 (2026-05-09): `greenlens-repository-pattern` cập nhật cho **hybrid pattern** (IRepository<T> base + aggregate-specific repo + IUnitOfWork). Thêm `base-pattern.cs.template` (one-time setup), `specific-repository.cs.template`, `repo-method-snippet.cs.template`, `handler-with-uow.cs.template`. `greenlens-feature-slice` templates cập nhật để inject `IXxxRepository + IUnitOfWork` thay vì `IApplicationDbContext`.
- v1.0: Phiên bản đầu.

Khi `OVERVIEW.md` hoặc BR doc đổi, cập nhật skill tương ứng (đặc biệt là rule mapping trong `greenlens-feature-slice` và `greenlens-efcore-best-practices`).

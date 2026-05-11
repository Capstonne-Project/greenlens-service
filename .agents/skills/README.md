# Greenlens Backend Skills (Workspace-specific)

Bộ skill workspace-specific cho dự án **SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution** (codename **Greenlens**), viết theo chuẩn Antigravity Skills.

Đi kèm `OVERVIEW.md` và `BusinessRules` của dự án. Bộ skill này hướng dẫn Claude scaffold mã backend .NET 9 Clean Architecture đúng convention mà không cần lặp lại hướng dẫn trong từng prompt.

## Có gì trong này

| Skill                                 | Trigger khi user yêu cầu…                                                                                                  |
| ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **`greenlens-domain-entity`**         | tạo entity Domain với state machine + domain events                                                                        |
| **`greenlens-feature-slice`**         | thêm command/query/use case/feature/handler/validator vào `Application/Features/`                                          |
| **`greenlens-repository-pattern`**    | thêm repository (hybrid: aggregate-specific + UoW) — push back nếu xin Generic Repository                                  |
| **`greenlens-mediatr-behavior`**      | pipeline behaviors: validation, transaction, logging, performance, caching, authorization                                  |
| **`greenlens-controller-base`**       | thêm endpoint/controller/route/action HTTP (kiểu `ControllerBase`)                                                         |
| **`greenlens-api-error-mapping`**     | mở rộng `ToActionResult` cho error type mới, custom ProblemDetails                                                         |
| **`greenlens-efcore-best-practices`** | bất cứ thay đổi nào liên quan EF Core — entity config, migration, query, indexing, geo (PostGIS), perf, audit, soft delete |
| **`greenlens-background-job`**        | Hangfire job với DI scope, retry policy, idempotency                                                                       |
| **`greenlens-integration-test`**      | integration test (Testcontainers Postgres) hoặc functional test (WebApplicationFactory)                                    |
| **`greenlens-vps-deployment`**        | deploy lên VPS Ubuntu 24.04 với Docker + Cloudflare Tunnel + GitHub Actions (Phase 1 IP-only → Phase 2 domain)             |

Mỗi skill là một thư mục độc lập với `SKILL.md` + `assets/` (template `.cs.template`) + (tuỳ skill) `references/` (doc chi tiết, load on-demand để giữ context nhỏ — đúng nguyên tắc _progressive disclosure_).

## Cấu trúc

```
skills/
├── greenlens-domain-entity/
│   ├── SKILL.md
│   └── assets/
│       ├── entity-base.cs.template          (Entity, AuditableEntity, SoftDeletableEntity, Result, Error — one-time)
│       ├── aggregate-root.cs.template
│       ├── sub-entity.cs.template
│       ├── value-object.cs.template
│       └── domain-event.cs.template
│
├── greenlens-feature-slice/
│   ├── SKILL.md
│   └── assets/
│       ├── command-slice.cs.template
│       └── query-slice.cs.template
│
├── greenlens-repository-pattern/
│   ├── SKILL.md
│   └── assets/
│       ├── base-pattern.cs.template          (IRepository, IUoW, EfRepository, EfUoW, TransactionBehavior — one-time)
│       ├── specific-repository.cs.template
│       ├── repo-method-snippet.cs.template
│       └── handler-with-uow.cs.template
│
├── greenlens-mediatr-behavior/
│   ├── SKILL.md
│   └── assets/
│       ├── validation-behavior.cs.template
│       ├── transaction-behavior.cs.template
│       ├── logging-behavior.cs.template      (also includes PerformanceBehavior)
│       ├── caching-behavior.cs.template      (with ICacheable marker)
│       ├── authorization-behavior.cs.template (with [RequireRole] attribute)
│       └── registration-snippet.cs.template
│
├── greenlens-controller-base/
│   ├── SKILL.md
│   └── assets/
│       ├── controller.cs.template
│       ├── action-snippet.cs.template
│       └── result-extensions.cs.template
│
├── greenlens-api-error-mapping/
│   ├── SKILL.md
│   └── assets/
│       ├── result-extensions-extended.cs.template
│       ├── error-type-additions.cs.template
│       └── validation-with-fields.cs.template
│
├── greenlens-efcore-best-practices/
│   ├── SKILL.md
│   ├── references/
│   │   ├── configuration.md
│   │   ├── queries-and-projection.md
│   │   ├── migrations.md
│   │   ├── geo-postgis.md
│   │   ├── performance.md
│   │   └── auditing-and-soft-delete.md
│   └── assets/
│       ├── entity-configuration.cs.template
│       ├── db-context.cs.template
│       ├── auditing-interceptor.cs.template
│       └── soft-delete-interceptor.cs.template
│
├── greenlens-background-job/
│   ├── SKILL.md
│   └── assets/
│       ├── recurring-job.cs.template
│       ├── one-off-job.cs.template
│       └── job-registration.cs.template
│
└── greenlens-integration-test/
│   ├── SKILL.md
│   └── assets/
│       ├── integration-fixture.cs.template      (PostgresContainerFixture, IntegrationTestBase — one-time)
│       ├── integration-test.cs.template
│       ├── functional-fixture.cs.template       (WebApplicationFactory — one-time)
│       ├── functional-test.cs.template
│       └── test-data-builder.cs.template
│
└── greenlens-vps-deployment/
    ├── SKILL.md
    ├── assets/
    │   ├── Dockerfile                            (multi-stage, non-root, healthchecked)
    │   ├── docker-compose.yml                    (API + Postgres 18 + Redis + migrator)
    │   ├── .env.production.template
    │   ├── cloudflared.service                   (systemd unit)
    │   ├── github-actions-deploy.yml             (.github/workflows/deploy.yml)
    │   └── ssh-hardening.sh                      (first-time setup script)
    └── references/
        ├── runbook-phase1-iponly.md              (chưa có domain — Cloudflare Tunnel)
        ├── runbook-phase2-domain.md              (migrate sang domain thật)
        └── troubleshooting.md
```

## Cài đặt vào Antigravity workspace

1. Trong workspace của dự án (cùng repo với backend), tạo thư mục `.agents/skills/`.
2. Copy 9 thư mục skill vào đó:

   ```bash
   cp -r skills/greenlens-* .agents/skills/
   ```

3. Reload workspace. Claude sẽ tự discover skill qua `SKILL.md` frontmatter và sử dụng đúng lúc.

> **Lưu ý:** đường dẫn cài đặt skill có thể khác giữa các phiên bản Antigravity. Logic skill (frontmatter + body) là chuẩn chung, không phụ thuộc path.

## Workflow điển hình khi build feature mới

Một use case mới đi qua các skill theo thứ tự (Claude tự chọn skill phù hợp khi user prompt):

```
1. greenlens-domain-entity        → tạo/sửa entity nếu cần
2. greenlens-efcore-best-practices → IEntityTypeConfiguration<T> + migration
3. greenlens-repository-pattern   → specific repo nếu cần (gate trước)
4. greenlens-feature-slice        → Command/Query + Handler + Validator
5. greenlens-controller-base      → endpoint HTTP
6. greenlens-mediatr-behavior     → nếu cần thêm cross-cutting (caching, etc.)
7. greenlens-background-job       → nếu use case cần job (notification, scheduled)
8. greenlens-integration-test     → test handler + endpoint
9. greenlens-api-error-mapping    → nếu cần error type mới
10. greenlens-vps-deployment      → đưa lên VPS (1 lần setup + auto qua GitHub Actions)
```

Không phải mọi feature đụng tất cả 10 skill. Một feature đơn giản có thể chỉ dùng feature-slice + controller-base + integration-test.

## Triết lý khi viết bộ skill này

1. **Skill nhỏ, trigger rõ.** Một skill cho một concern. Description nêu rõ các phrasing user thường dùng (kể cả casual: "fix the N+1", "add an endpoint") — chống _under-triggering_.
2. **Progressive disclosure.** SKILL.md ngắn (< 500 dòng); chi tiết nặng được tách sang `references/` và chỉ load on-demand. Skill EF Core áp dụng pattern này.
3. **Templates với placeholder rõ ràng.** Mọi placeholder dùng `__DOUBLE_UNDERSCORE__` để find-replace toàn cục an toàn.
4. **Skill là _gatekeeper_, không chỉ là _generator_.** `repository-pattern` push back khi user xin Generic Repository. `mediatr-behavior` push back khi logic chỉ thuộc 1 handler. `api-error-mapping` push back khi user định try/catch trong controller. `domain-entity` push back khi user xin anemic entity. `integration-test` push back khi user định mock DbContext.
5. **Mọi BR ID đều bắt nguồn từ tài liệu chuẩn.** Skill yêu cầu Claude _hỏi_ khi không chắc BR ID — không bịa. Mỗi handler/job/test đều có comment `Implements: BR-...` để team grep được coverage.
6. **Convention là một sản phẩm, không phải sở thích.** Skill enforce convention từ `OVERVIEW.md`. Mỗi quy tắc đều có lý do — skill nêu lý do, không chỉ ra lệnh.

## Khi nào KHÔNG dùng các skill này

- Dự án khác (không phải SU26SE049). Bộ skill này hard-code namespace `Greenlens`, BR module prefix, Cloudflare config. Fork và tổng quát hoá nếu bạn muốn dùng cho dự án khác.
- Frontend, mobile, hoặc Infrastructure-as-Code. Bộ này chỉ cover backend .NET.
- Quyết định kiến trúc lớn (đổi từ PostgreSQL sang SQL Server, đổi MediatR sang một mediator khác, đổi Cloudflare sang AWS). Skill chỉ thực thi convention đã chốt; thay đổi convention là thảo luận với team trước, sau đó cập nhật `OVERVIEW.md` + skill cùng nhau.

## Mở rộng

Cần skill mới? Pattern đã ổn:

- Một `SKILL.md` với YAML frontmatter (`name`, `description` "đẩy" — nói rõ các phrasing trigger cả casual lẫn kỹ thuật).
- `assets/*.cs.template` với placeholder `__SCREAMING__`.
- Nếu skill phức tạp, thêm `references/<topic>.md` và trỏ trong SKILL.md theo bảng "load reference khi…".

Skill ý tưởng nếu mở rộng tiếp:

- `greenlens-cloudflare-r2` — presigned URL flow, content validation, lifecycle rules
- `greenlens-turnstile-flow` — verify token end-to-end (FE + BE handler)
- `greenlens-domain-event-handler` — INotificationHandler<TEvent> với Outbox
- `greenlens-realtime-signalr` — nếu thêm SignalR hub cho live updates
- `greenlens-feature-flag` — Microsoft.FeatureManagement integration

---

**Phiên bản:** 2.1
**Đồng bộ với:** `OVERVIEW.md` v1.2 + `SU26SE049_BusinessRules_v1_0.docx` v1.0 (17/04/2026).
**Changelog:**

- v2.1 (2026-05-10): Thêm `greenlens-vps-deployment` — deploy lên Ubuntu 24.04 VPS với Docker Compose + Cloudflare Tunnel + GitHub Actions. Cover 2 phase: IP-only (chưa có domain, dùng `*.cfargotunnel.com`) và Domain (sau khi mua). Bao gồm Dockerfile multi-stage, compose với PostGIS+Redis, SSH hardening script, runbook step-by-step.
- v2.0 (2026-05-09): Rename namespace `ecoreport-*` → `greenlens-*` để khớp project name. Thêm 5 skill mới: `greenlens-domain-entity`, `greenlens-mediatr-behavior`, `greenlens-background-job`, `greenlens-integration-test`, `greenlens-api-error-mapping`. Tổng 9 skill phủ trọn vẹn workflow build feature.
- v1.1 (2026-05-09): `repository-pattern` cập nhật cho **hybrid pattern** (IRepository<T> base + aggregate-specific repo + IUnitOfWork).
- v1.0: Phiên bản đầu (4 skill `ecoreport-*`).

Khi `OVERVIEW.md` hoặc BR doc đổi, cập nhật skill tương ứng.

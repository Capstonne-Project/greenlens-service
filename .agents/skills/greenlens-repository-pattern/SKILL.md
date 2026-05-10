---
name: greenlens-repository-pattern
description: Scaffold an aggregate-specific repository (extending IRepository<T>) and/or wire it into a handler with IUnitOfWork in the GreenLens .NET 9 backend (project SU26SE049). Use this skill whenever the user asks for a "repository", "repo", "data access layer", "DAL", "IXxxRepository", "UnitOfWork", "UoW", or talks about adding query/persistence methods for an aggregate — including casual phrasings like "add a repo for reports", "I need IUserRepository", "add GetForVerificationAsync to ReportRepository", "wire UoW into this handler". Trigger this even when the user only mentions UoW or only mentions a repo — they go together in this project. Pushes back if the request is for a generic repository (project chose hybrid: specific repo + UoW per OVERVIEW.md §4.12). Produces the interface in Application/Common/Interfaces/Persistence/, the EF implementation extending EfRepository<T> in Infrastructure/Persistence/Repositories/, plus DI registration.
---

# GreenLens Repository + Unit of Work (Hybrid Pattern)

The GreenLens backend uses a **hybrid pattern** per `OVERVIEW.md` §4.12:

- `IRepository<T>` — base contract, mỏng, mỗi aggregate root đều có
- `IUnitOfWork` — commit boundary, handler không trực tiếp gọi `DbContext.SaveChangesAsync`
- **Aggregate-specific repository** (`IReportRepository`, `IUserRepository`) extends `IRepository<T>` + thêm method nghiệp vụ
- **Generic Repository thuần KHÔNG được dùng** — tức là KHÔNG `services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))`

This skill scaffolds either piece (repo + DI registration) or wires UoW into handler examples.

## When to use

Trigger when the user mentions:
- "repository", "repo", "DAL", `IXxxRepository`
- "UnitOfWork", "UoW", "transaction boundary"
- "Should I add a method to IReportRepository?"
- "Wire UoW into this handler"
- "Generic repository", "base repository" — **push back** (see Step 0)

## Step 0 — Push back if user asks for Generic Repository

If the user says any of:
- "Add a generic IRepository<T>"
- "Register the generic repo in DI"
- "Use IRepository<Report> directly in the handler"

Stop and explain:

> "Project chose hybrid pattern (OVERVIEW.md §4.12): `IRepository<T>` base contract + aggregate-specific repos (`IReportRepository`). Generic repo registration was explicitly rejected — handler should depend on `IReportRepository`, not `IRepository<Report>`. The base interface exists for inheritance + test consistency, not for direct DI use."

Then ask: "Want me to scaffold `IReportRepository` instead?"

## Step 1 — Decide what to scaffold

Three common asks:

### A) Add a new aggregate-specific repository

User says "I need IUserRepository". Confirm:
- Aggregate name (`User`, `Report`, `CleanupTeam`, ...)
- **What methods belong on the repo?** Each one must justify its existence. The base interface already gives `Query`, `QueryAsNoTracking`, `GetByIdAsync`, `AddAsync`, `Remove`, `ExistsAsync` — DO NOT re-declare them.
- A method belongs on the repo if:
  - It bundles `Include(...)` chains used by 3+ handlers, OR
  - It needs PostGIS / raw SQL / window functions not expressible in clean LINQ, OR
  - It encodes a business invariant on loading (e.g. always load aggregate with its child collection)
- A method does NOT belong if it's a one-line LINQ that 1 handler uses — leave in the handler.

Use `assets/specific-repository.cs.template`.

### B) Add a method to existing repo

User says "Add `GetForVerificationAsync` to `IReportRepository`". Confirm it passes the same justification test above. Use `assets/repo-method-snippet.cs.template`.

### C) Wire UoW into a handler

User says "Use UoW in `VerifyReportCommandHandler`". Use `assets/handler-with-uow.cs.template`. Common patterns:
- Single-aggregate mutation (most cases)
- Multi-aggregate transaction (verify report + award points to citizen)
- Domain event raise → handled after `uow.SaveChangesAsync()`

## Step 2 — Verify base scaffolding exists

Before generating a specific repo, check that the project has:
- `IRepository<T>` in `Application/Common/Interfaces/Persistence/`
- `IUnitOfWork`, `IDbTransaction` in same folder
- `EfRepository<T>` (internal abstract) in `Infrastructure/Persistence/Repositories/`
- `EfUnitOfWork` in `Infrastructure/Persistence/`
- `TransactionBehavior` in `Application/Common/Behaviors/`

If any are missing, materialize from `assets/base-pattern.cs.template` first (one-time per project).

## Step 3 — Self-check

- [ ] Specific repo interface lives in `Application/Common/Interfaces/Persistence/`, NOT in `Domain/`
- [ ] EF impl is `internal sealed`, kế thừa `EfRepository<T>` + interface cụ thể
- [ ] Interface KHÔNG re-declare `GetByIdAsync`, `AddAsync`, ... (đã có ở base)
- [ ] Mỗi method mới có XML doc giải thích vì sao nó thuộc về repo (justification trong Step 1)
- [ ] DI registration: `services.AddScoped<IXxxRepository, XxxRepository>();` — KHÔNG generic registration
- [ ] Handler nhận `IXxxRepository` (specific), KHÔNG `IRepository<X>`
- [ ] Handler gọi `await uow.SaveChangesAsync(ct)` thay vì repo có method SaveChanges
- [ ] Mọi async method có `CancellationToken` parameter
- [ ] Method query trả `IReadOnlyList<T>` hoặc `T?`, KHÔNG `IQueryable<T>` (handler dùng `Query()` cho ad-hoc)

## Templates

- `assets/base-pattern.cs.template` — IRepository<T>, IUnitOfWork, EfRepository<T>, EfUnitOfWork, TransactionBehavior (one-time setup)
- `assets/specific-repository.cs.template` — IXxxRepository + XxxRepository + DI snippet
- `assets/repo-method-snippet.cs.template` — single method to add to existing repo
- `assets/handler-with-uow.cs.template` — handler patterns with UoW

## Common follow-up traps

- User: "Add `GetByEmailAsync` to `IUserRepository`" → check: does any handler also need to query by email? If yes, OK (it's reused). If only 1 handler, push back: "That's a one-liner — `users.QueryAsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct)` in the handler is enough."
- User: "Add `SaveChangesAsync` to `IReportRepository`" → push back: "Commit goes through `IUnitOfWork`, not the repo. Handler injects both."
- User: "Make a generic `BaseRepository<T>`" → STOP. The project's `EfRepository<T>` is `internal abstract` precisely to prevent this. Re-explain Step 0.

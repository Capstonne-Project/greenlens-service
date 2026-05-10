---
name: greenlens-feature-slice
description: Scaffold a new vertical-slice feature (CQRS Command or Query) in the GreenLens .NET 9 Clean Architecture backend (project SU26SE049). Use this skill whenever the user asks to add a new use case, endpoint, command, query, handler, or feature to the backend — even casual phrasings like "make an endpoint for verifying reports", "add a command to assign cleanup team", "I need a query to fetch nearby reports", or "create the submit report feature". Trigger this even when the user only mentions one piece (e.g. "just write the validator for X") because the slice should stay consistent. Produces the full 4-file slice (Command/Query, Handler, Validator, Response DTO) with proper folder placement under Application/Features/<Module>/<UseCase>/, MediatR + FluentValidation + Result<T> patterns, BR-ID XML comments, and CancellationToken propagation.
---

# GreenLens Feature Slice (CQRS)

This skill scaffolds a single vertical slice for the GreenLens backend (project **SU26SE049 — Crowdsourced Application for Reporting Environmental Pollution**). It enforces the conventions in the workspace `OVERVIEW.md` so every feature looks the same.

## When to use

Trigger this skill any time a new use case is added to `GreenLens.Application/Features/`. Even if the user asks for "just a handler" or "just an endpoint", produce the full slice — partial slices drift from the project standard and force rework later.

If the user asks for an endpoint **and** an entity that does not exist yet, do the entity first (separate request), then come back to the slice. Slices assume the Domain entity already exists.

## Workflow

1. **Clarify before writing files.** Ask only what you cannot infer:
   - Module folder (e.g. `Reports`, `Auth`, `Officer`, `Cleanup`, `Map`, `Admin`, `Gamification`, `Notifications`, `Comments`, `Analytics`)
   - Use-case name in PascalCase (e.g. `SubmitReport`, `VerifyReport`, `AssignCleanupTeam`)
   - Kind: **Command** (mutates) or **Query** (read-only)
   - Return type (`Guid`, a DTO record, `PagedList<T>`, `Unit` for fire-and-forget)
   - Which **BR IDs** the slice implements (look them up in `SU26SE049_BusinessRules_v1_0.docx`; if uncertain, ask the user — do not invent IDs)
   - Authorization: anonymous / Citizen / Officer / CleanupTeam / Admin / multiple
2. **Pick the right template** from `assets/`:
   - `command-slice.cs.template` for Commands
   - `query-slice.cs.template` for Queries
   - Both contain the four files concatenated with `// === FILE: <path> ===` markers — split on the markers when materializing.
3. **Substitute placeholders.** Every placeholder is wrapped in `__DOUBLE_UNDERSCORES__` so a global find-replace is safe:
   - `__MODULE__` — e.g. `Reports`
   - `__USECASE__` — PascalCase, e.g. `SubmitReport`
   - `__USECASE_LOWER__` — camelCase, e.g. `submitReport` (used only inside doc text)
   - `__RESULT_TYPE__` — e.g. `Guid`, `SubmitReportResponse`, `PagedList<ReportListItemDto>`
   - `__BR_IDS__` — comma-separated list, e.g. `BR-REP-001, BR-REP-003, BR-REP-005, BR-REP-010, BR-REP-013`
   - `__BR_SUMMARY__` — one-line human summary, e.g. `Submit a new pollution report with photos and GPS.`
4. **Write each file to its target path** under `src/GreenLens.Application/Features/__MODULE__/__USECASE__/`.
5. **Do NOT also generate the endpoint here.** The endpoint belongs in `GreenLens.Api/Endpoints/` (or `Controllers/` if the project uses controllers — check before assuming). If the user wants the endpoint too, hand off to `greenlens-controller-base` afterwards.
6. **Stop and ask** if you are about to:
   - Reference an entity, enum, or repository that does not yet exist in `GreenLens.Domain` or `GreenLens.Application/Common/Interfaces/`
   - Implement a BR rule whose wording is ambiguous in the BR doc
   - Cross-cut into Infrastructure (e.g. you need a new `IFileStorage` method) — that is a separate change

## File layout produced

```
src/GreenLens.Application/Features/__MODULE__/__USECASE__/
├── __USECASE__Command.cs           (or __USECASE__Query.cs)
├── __USECASE__CommandHandler.cs    (or Query handler)
├── __USECASE__CommandValidator.cs  (or Query validator)
└── __USECASE__Response.cs          (only if __RESULT_TYPE__ is a custom DTO)
```

Skip `Response.cs` when the return type is a primitive (`Guid`, `bool`) or a DTO that already lives elsewhere (e.g. a shared `ReportDto`).

## Conventions enforced (matches `CLAUDE.md` §4.1–§4.3)

- `record` for Command / Query / Response — they are immutable.
- Handler is `sealed class`, primary constructor for dependencies, **no field underscores** beyond what's needed.
- Returns `Result<T>` from `GreenLens.Domain.Common.Result` — never throw for business failures, only for infrastructure faults.
- `CancellationToken` is the last parameter on every async method, propagated to every `await`.
- BR IDs go in an XML `<remarks>` block on the handler. Format exactly as in the template — the team greps for `BR-` to build coverage reports.
- Validator inherits from `AbstractValidator<TCommand>`. Input-shape validation only (lengths, regex, ranges). DB-touching rules (uniqueness, state transitions, role checks) live **in the handler**, not the validator (CLAUDE.md §4.9).
- Use `IApplicationDbContext` (not concrete `ApplicationDbContext`) and `ICurrentUser` from `Application/Common/Interfaces/`.
- For paginated queries, return `PagedList<TDto>` and accept `int Page = 1, int PageSize = 20` with a hard cap of 100 (rate-limit hygiene per BR-SYS-004).

## Self-check before handing files back

Run this checklist in your head; do not skip it:

- [ ] Folder name matches `__USECASE__` exactly (PascalCase)
- [ ] Filename suffix is correct: `Command` / `Query` / `CommandHandler` / `QueryHandler` / `CommandValidator` / `QueryValidator`
- [ ] BR IDs in XML comment match the IDs the user (or BR doc) confirmed — never guessed
- [ ] Handler does not throw `DomainException` for business rule violations — returns `Result.Failure(Error.X)` instead
- [ ] No `using Microsoft.EntityFrameworkCore` at the top of the Command/Query/Validator/Response files; only the Handler may use it (and only via `IApplicationDbContext`)
- [ ] No `IHttpContextAccessor`, no `HttpContext` anywhere — use `ICurrentUser`
- [ ] `CancellationToken` flows through every async call

## Example interaction

**User:** "Add a command to mark a cleanup task resolved with before/after photos."

**Your response:**
1. Confirm: module = `Cleanup`, use-case = `MarkTaskResolved`, BR IDs = `BR-REP-014, BR-REP-023, BR-CLN-004` (state transition + 2 distinct after photos).
2. Confirm return type: `Guid` (the report ID, since caller already has the task ID).
3. Confirm authorization: `CleanupTeam` only.
4. Materialize the slice from `command-slice.cs.template`.
5. Tell the user: "Slice written. The endpoint isn't included — want me to add it next? That would use the `greenlens-controller-base` skill."

## Templates

- `assets/command-slice.cs.template` — full 4-file Command slice
- `assets/query-slice.cs.template` — full 4-file Query slice (paginated + non-paginated variants in comments)

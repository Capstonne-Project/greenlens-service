---
name: greenlens-plan
description: >
  Produces a scope document and milestone checklist before any code changes begin.
  Use this skill at the START of every new feature, bugfix, or refactoring task in the
  GreenLens Pollution Reporting backend (.NET 9 / ASP.NET Core / Clean Architecture).
  Triggers: user asks to "plan", "scope", "design", "propose", or starts a new feature/epic.
---

# GreenLens — Plan Step

> **Goal:** Produce a clear scope document **and** a milestone checklist so the team can review before any code is written.

## When to use this skill

- A new feature request arrives (e.g. "implement report verification flow").
- A bug or performance issue needs investigation before fixing.
- A refactoring or architectural change is proposed.
- Before any PR that touches ≥ 3 files or crosses layer boundaries.

## How to use it

### 1. Gather Context

1. **Identify Business Rules** — Look up related `BR-<MODULE>-<NNN>` IDs from `CLAUDE.md §5` and the BR docx. List every rule that the change touches.
2. **Identify Actors** — Which of the 6 actors (Citizen, Environmental Officer, Cleanup Team, System Administrator, AI Service, Community Organization) are involved?
3. **Map to Vertical Slice** — Determine which `Application/Features/<Module>/` folder(s) this change belongs to. If new, define the slice name now.
4. **Check Dependency Rule** — Confirm the change respects:
   ```
   Api ──► Application ──► Domain
    │           │
    └──► Infrastructure ──► Application (interfaces) ──► Domain
   ```
   Domain MUST NOT reference any other project. No `Microsoft.*` in Domain.
5. **Non-functional targets** — Check if the change impacts: p95 < 2s, 5,000 CCU, uptime ≥ 99.5%, RPO ≤ 24h/RTO ≤ 4h, i18n vi-VN/en-US.

### 2. Produce Scope Document

Create a markdown document with these sections:

```markdown
# [Feature/Task Name]

## Summary
One-paragraph description of what this change accomplishes.

## Business Rules Covered
| BR ID | Description | Implementation Location |
|-------|-------------|------------------------|
| BR-XXX-NNN | ... | Application/Features/... |

## Actors Affected
- [ ] Citizen
- [ ] Environmental Officer
- [ ] Cleanup Team
- [ ] System Administrator
- [ ] AI Service
- [ ] Community Organization

## Architectural Impact
- **Domain changes:** (entities, value objects, events, state machine transitions)
- **Application changes:** (commands, queries, validators, handlers)
- **Infrastructure changes:** (DB config, external adapters, background jobs)
- **API changes:** (new/modified endpoints, auth policies)
- **Migration required:** Yes/No — name: `yyyyMMddHHmm_VerbNoun`

## State Machine Impact (if applicable)
Document any new transitions in the Report state machine:
```
Submitted ──► Verified ──► InProgress ──► Resolved ──► Closed
```

## API Contract
- **Method + Path:** `POST /v1/reports`
- **Auth:** Bearer JWT, Policy: `Policies.CanSubmitReport`
- **Request body / Query params:** (shape)
- **Response envelope:** `{code, message, status, data}` per 00_API_CONVENTIONS.md §2
- **Error codes:** List relevant business codes (e.g., `INVALID_GPS`, `DUPLICATE_REPORT`)

## Dependencies & Risks
| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| ... | Low/Med/High | ... |

## Out of Scope
Explicitly list what this change does NOT cover.
```

### 3. Produce Milestone Checklist

Break the work into ordered, testable milestones:

```markdown
## Milestone Checklist

- [ ] **M1: Domain** — Entity/VO/Event changes + unit tests
- [ ] **M2: Application** — Command/Query/Handler/Validator + unit tests
- [ ] **M3: Infrastructure** — DB config, migration, adapters
- [ ] **M4: API** — Controller endpoint, auth policy, middleware
- [ ] **M5: Integration Tests** — Testcontainers Postgres
- [ ] **M6: Functional Tests** — WebApplicationFactory E2E
- [ ] **M7: Documentation** — Swagger annotations, Postman collection
- [ ] **M8: Review** — BR IDs in commit message, PR template filled
```

### 4. Validation Checklist (before proceeding to Build)

- [ ] Every BR ID listed has been verified against the source docx
- [ ] Scope does not introduce new packages without user approval
- [ ] Migration naming follows `yyyyMMddHHmm_VerbNoun` convention
- [ ] API contract follows `00_API_CONVENTIONS.md` response envelope
- [ ] No `using Microsoft.EntityFrameworkCore` in Domain or Application (strict \u2014 NO exceptions)
- [ ] Handlers use `IXxxRepository` + `IUnitOfWork`, never `IApplicationDbContext`
- [ ] User has approved the scope document

> **STOP** — Do not proceed to the Build step until the user has reviewed and approved this plan.

## Sources & References

| Source | Path | Description |
|--------|------|-------------|
| OVERVIEW.md | `OVERVIEW.md` | Project overview, actors, BR mapping, architecture, coding standards |
| API Conventions | `00_API_CONVENTIONS.md` | Response envelope, status codes, business codes, pagination |
| Business Rules | `SU26SE049_BusinessRules_v1_0.docx` | Source of truth for all BR-*-NNN rules |
| State Machine | `OVERVIEW.md §5` | Report state transitions (BR-REP-020, BR-REP-021) |
| Folder Structure | `../csharp-conventions/resources/folder-structure.md` | Full solution tree and "where things go" table |


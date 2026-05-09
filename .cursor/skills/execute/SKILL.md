---
name: execute
description: >
  Slash command `/execute` — full feature delivery pipeline for the GreenLens .NET 9 backend.
  Chains scout → api-actor → performance → security → test subagents to take a feature from
  scoping to merge-ready in one workflow. Each stage runs in its own context window so the parent
  agent stays focused. Use when the user asks to "execute", "deliver", "implement end-to-end",
  "ship feature", "full pipeline".
disable-model-invocation: true
---

# /execute — Full Feature Delivery Pipeline

> Chain: **scout → api-actor → performance → security → test**
>
> Each stage is a Task call to the matching subagent in `.cursor/agents/`, so each one gets a
> fresh context window and the parent agent only carries the summaries forward.

## Inputs

Ask the user for these BEFORE starting (skip questions already answered in the conversation):

1. **Feature title** — one line.
2. **BR IDs in scope** — list from `CLAUDE.md §5` or the BR docx.
3. **API contract** — method, path, request/response shape, key error codes.
4. **Acceptance criteria** — what "done" looks like.

If any are missing, use `AskQuestion` once. Do NOT proceed without them.

## Pipeline

Run the stages **sequentially**. Carry forward only the structured summary from each stage.

### Stage 1 — `scout` (read-only reconnaissance)

```
Task(subagent_type="scout",
     prompt="""Scout the codebase for this feature:

Title: <feature title>
BR IDs: <list>
API contract: <method + path + request + response + error codes>

Return the scout report per your output template. List every file the parent agent will need to touch and every BR gap.""")
```

Carry forward: scout report (BR IDs, vertical slice path, files of interest, risks).

### Stage 2 — `api-actor` (implementation)

```
Task(subagent_type="api-actor",
     prompt="""Implement the vertical slice end-to-end.

Scout report (use as ground truth):
<paste scout report>

Acceptance criteria:
<paste criteria>

Follow your strict implementation order: Domain → Application → Infrastructure → Api.
Run `dotnet build` and report failures immediately.""")
```

Carry forward: list of files changed, BR IDs implemented, migration name, new interfaces in Application that need infra implementations.

### Stage 3 — `performance` (audit)

```
Task(subagent_type="performance",
     prompt="""Audit the changes for performance issues.

Files changed:
<list from api-actor>

Check the full performance audit checklist. Report critical/medium/low findings with file:line.""")
```

If critical findings exist → loop back to `api-actor` with the findings to fix BEFORE continuing. Repeat until no criticals.

### Stage 4 — `security` (audit)

```
Task(subagent_type="security",
     prompt="""Audit the changes for security issues.

Files changed:
<list from api-actor>

BR IDs touched:
<list>

Check the full security audit checklist. Report critical/medium/low findings with OWASP and BR mapping.""")
```

If critical findings exist → loop back to `api-actor` (or `fix` if scope is small). Repeat until no criticals.

### Stage 5 — `test` (write + run)

```
Task(subagent_type="test",
     prompt="""Write and run tests for the new vertical slice.

BR IDs that MUST be covered:
<list>

Files changed:
<list>

Follow the pyramid (~70/25/5). Use Testcontainers for any DB-shaped logic. Return the BR coverage report and any test failures.""")
```

If tests fail → triage:
- Production code bug → loop back to `fix`.
- Test bug → `test` agent fixes and re-runs.
- Environment → document and stop.

## Final report

Once all stages complete cleanly, produce:

```markdown
# /execute — <feature title>

## Status: ✅ Ready for review

## Stages
- ✅ Scout: <N> files identified, <M> BRs in scope
- ✅ API Actor: <N> files changed, migration `yyyyMMddHHmm_VerbNoun`
- ✅ Performance: 0 critical, <N> medium recommendations (filed as follow-ups)
- ✅ Security: 0 critical, <N> medium recommendations (filed as follow-ups)
- ✅ Test: <N>/<N> passing, BR coverage 100%

## BR coverage
| BR ID | Implementation | Test |
|-------|---------------|------|
| BR-XXX-NNN | ✅ | ✅ |

## Suggested commit message
feat(<scope>): <one-line> (BR-XXX-NNN, BR-XXX-MMM)

## Open follow-ups (not blocking)
- ...
```

## Stop conditions

- Critical security finding that the user must triage → STOP, surface to user, do not loop indefinitely.
- BR ambiguity (`scout` flagged a missing BR or contradiction) → STOP, ask user.
- `dotnet build` fails twice in a row in the same stage → STOP, surface stderr.

## What this skill does NOT do

- Does not push to remote / open PR (that's a separate `release` step).
- Does not modify infrastructure (CI, Docker, Hangfire schedule) without user approval.
- Does not invent BR rules.
